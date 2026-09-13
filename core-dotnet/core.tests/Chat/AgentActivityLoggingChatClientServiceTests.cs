using System.Runtime.CompilerServices;
using Core.Agent.Events;
using Core.Chat.Models;
using Core.Chat.Services;
using Core.Data.Domain;
using CQMediator;

namespace Core.Tests.Chat;

public class AgentActivityLoggingChatClientServiceTests
{
    private sealed class ScriptedChatClientService(Func<CancellationToken, IAsyncEnumerable<ChatStreamEvent>> script) : IChatClientService
    {
        public IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(ChatSendMessageRequest request, CancellationToken cancellationToken) =>
            script(cancellationToken);
    }

    private sealed class RecordingMediator : IMediator
    {
        public List<LogAgentActivityEvent> LoggedEvents { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is LogAgentActivityEvent logEvent)
            {
                LoggedEvents.Add(logEvent);
                return Task.FromResult((TResponse)(object)Guid.NewGuid());
            }

            throw new NotSupportedException($"Unexpected request type: {request.GetType()}");
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static async IAsyncEnumerable<ChatStreamEvent> YieldEvents(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        params ChatStreamEvent[] events)
    {
        foreach (var streamEvent in events)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return streamEvent;
        }
    }

    private static async IAsyncEnumerable<ChatStreamEvent> YieldThenThrow(
        [EnumeratorCancellation] CancellationToken cancellationToken,
        ChatStreamEvent[] events,
        Exception exception)
    {
        foreach (var streamEvent in events)
        {
            await Task.Yield();
            yield return streamEvent;
        }

        throw exception;
    }

    private static async Task<List<ChatStreamEvent>> DrainAsync(IAsyncEnumerable<ChatStreamEvent> source)
    {
        var events = new List<ChatStreamEvent>();
        await foreach (var streamEvent in source)
        {
            events.Add(streamEvent);
        }

        return events;
    }

    [Fact]
    public async Task SendMessageAsync_HappyPath_LogsPairedRequestAndResponseWithRealSessionId()
    {
        var inner = new ScriptedChatClientService(ct => YieldEvents(
            ct,
            ChatStreamEvent.Session("session-123"),
            ChatStreamEvent.Token("Hello"),
            ChatStreamEvent.Done(new ChatUsage { InputTokenCount = 1, OutputTokenCount = 2, RuntimeMs = 10 })));
        var mediator = new RecordingMediator();
        var decorator = new AgentActivityLoggingChatClientService(inner, mediator, "Chat1a", AgentActivityFeatureCategory.ModelDirect);

        var events = await DrainAsync(decorator.SendMessageAsync(new ChatSendMessageRequest { Message = "Hi" }, CancellationToken.None));

        Assert.Equal(3, events.Count);
        Assert.Equal(2, mediator.LoggedEvents.Count);

        var requestLog = mediator.LoggedEvents[0];
        var responseLog = mediator.LoggedEvents[1];
        Assert.Equal(AgentActivityDirection.Request, requestLog.Direction);
        // The Request row is logged lazily on the first observed event (always "session"), so a
        // brand-new chat's very first row gets the real assigned SessionId, not an empty string.
        Assert.Equal("session-123", requestLog.SessionId);
        Assert.Equal(AgentActivityDirection.Response, responseLog.Direction);
        Assert.Equal("Hello", responseLog.Content);
        Assert.Equal(10, responseLog.RuntimeMs);
        Assert.Equal(requestLog.RunId, responseLog.RunId);
    }

    [Fact]
    public async Task SendMessageAsync_ToolStartAndEnd_LoggedAsPairedNestedRowsWithLoopNumber()
    {
        var inner = new ScriptedChatClientService(ct => YieldEvents(
            ct,
            ChatStreamEvent.Session("session-1"),
            ChatStreamEvent.ToolStart("GetLatLong", "{\"location\":\"Nashville\"}"),
            ChatStreamEvent.ToolEnd("GetLatLong", "{\"location\":\"Nashville\"}", "{\"lat\":36}"),
            ChatStreamEvent.Done(null)));
        var mediator = new RecordingMediator();
        var decorator = new AgentActivityLoggingChatClientService(inner, mediator, "Chat1a", AgentActivityFeatureCategory.ModelDirect);

        await DrainAsync(decorator.SendMessageAsync(new ChatSendMessageRequest { Message = "Where is Nashville?" }, CancellationToken.None));

        // turn Request, tool Request, tool Response, turn Response
        Assert.Equal(4, mediator.LoggedEvents.Count);
        var toolRequest = mediator.LoggedEvents[1];
        var toolResponse = mediator.LoggedEvents[2];

        Assert.Equal("GetLatLong", toolRequest.ToolName);
        Assert.Null(toolRequest.AgentName);
        Assert.Equal(1, toolRequest.LoopNumber);
        Assert.NotNull(toolResponse.CorrelationId);
        Assert.Equal("GetLatLong", toolResponse.ToolName);
        Assert.Equal(1, toolResponse.LoopNumber);
    }

    [Fact]
    public async Task SendMessageAsync_MultiAgentCategory_LogsDelegationCallAsAgentNameNotToolName()
    {
        var inner = new ScriptedChatClientService(ct => YieldEvents(
            ct,
            ChatStreamEvent.Session("session-1"),
            ChatStreamEvent.ToolStart("Geo", "{\"query\":\"Nashville\"}"),
            ChatStreamEvent.ToolEnd("Geo", "{\"query\":\"Nashville\"}", "36.16,-86.78"),
            ChatStreamEvent.Done(null)));
        var mediator = new RecordingMediator();
        var decorator = new AgentActivityLoggingChatClientService(inner, mediator, "Chat4a", AgentActivityFeatureCategory.MultiAgent);

        await DrainAsync(decorator.SendMessageAsync(new ChatSendMessageRequest { Message = "Weather in Nashville" }, CancellationToken.None));

        var toolRequest = mediator.LoggedEvents[1];
        Assert.Equal("Geo", toolRequest.AgentName);
        Assert.Null(toolRequest.ToolName);
    }

    [Fact]
    public async Task SendMessageAsync_ErrorEvent_LogsResponseRowWithErrorMessage()
    {
        var inner = new ScriptedChatClientService(ct => YieldEvents(
            ct,
            ChatStreamEvent.Session("session-1"),
            ChatStreamEvent.Error("Foundry auth failed")));
        var mediator = new RecordingMediator();
        var decorator = new AgentActivityLoggingChatClientService(inner, mediator, "Chat1a", AgentActivityFeatureCategory.ModelDirect);

        await DrainAsync(decorator.SendMessageAsync(new ChatSendMessageRequest { Message = "Hi" }, CancellationToken.None));

        Assert.Equal(2, mediator.LoggedEvents.Count);
        var responseLog = mediator.LoggedEvents[1];
        Assert.Equal(AgentActivityDirection.Response, responseLog.Direction);
        Assert.Equal("Foundry auth failed", responseLog.ErrorMessage);
    }

    [Fact]
    public async Task SendMessageAsync_InnerThrowsBeforeAnyEvent_StillLogsPairedRequestAndErrorResponseThenRethrows()
    {
        var inner = new ScriptedChatClientService(ct =>
            YieldThenThrow(ct, [], new InvalidOperationException("boom")));
        var mediator = new RecordingMediator();
        var decorator = new AgentActivityLoggingChatClientService(inner, mediator, "Chat1a", AgentActivityFeatureCategory.ModelDirect);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DrainAsync(decorator.SendMessageAsync(new ChatSendMessageRequest { Message = "Hi" }, CancellationToken.None)));
        Assert.Equal("boom", ex.Message);

        // No "session" event was ever seen, so this Request row is logged retroactively in the
        // catch, with whatever SessionId the caller supplied (empty here) -- an unpaired Request
        // row is never left behind even when the inner service fails before yielding anything.
        Assert.Equal(2, mediator.LoggedEvents.Count);
        Assert.Equal(AgentActivityDirection.Request, mediator.LoggedEvents[0].Direction);
        Assert.Equal(AgentActivityDirection.Response, mediator.LoggedEvents[1].Direction);
        Assert.Equal("boom", mediator.LoggedEvents[1].ErrorMessage);
        Assert.Equal(mediator.LoggedEvents[0].RunId, mediator.LoggedEvents[1].RunId);
    }

    [Fact]
    public async Task SendMessageAsync_InnerThrowsMidStream_LogsErrorResponseUsingRealSessionIdThenRethrows()
    {
        var inner = new ScriptedChatClientService(ct => YieldThenThrow(
            ct,
            [ChatStreamEvent.Session("session-1"), ChatStreamEvent.Token("partial")],
            new InvalidOperationException("network blip")));
        var mediator = new RecordingMediator();
        var decorator = new AgentActivityLoggingChatClientService(inner, mediator, "Chat1a", AgentActivityFeatureCategory.ModelDirect);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => DrainAsync(decorator.SendMessageAsync(new ChatSendMessageRequest { Message = "Hi" }, CancellationToken.None)));

        Assert.Equal(2, mediator.LoggedEvents.Count);
        Assert.Equal("session-1", mediator.LoggedEvents[0].SessionId);
        Assert.Equal("network blip", mediator.LoggedEvents[1].ErrorMessage);
        Assert.Equal("partial", mediator.LoggedEvents[1].Content);
    }
}
