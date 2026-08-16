using System.Text;
using System.Text.Json;
using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.Chat.Chat1a;

public sealed class Chat1aService : IChatClientService
{
    private readonly IChatSessionStore _sessionStore;
    private readonly ChatToolExecutor _toolExecutor;
    private readonly ChatFoundrySettings _settings;
    private readonly ILogger<Chat1aService> _logger;

    public Chat1aService(
        IChatSessionStore sessionStore,
        ChatToolExecutor toolExecutor,
        ChatFoundrySettings settings,
        ILogger<Chat1aService> logger)
    {
        _sessionStore = sessionStore;
        _toolExecutor = toolExecutor;
        _settings = settings;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sessionId = ChatResponsesSessionHelper.ResolveSessionId(
            _sessionStore,
            ChatResponsesSessionHelper.Chat1aKind,
            request.SessionId);

        yield return ChatStreamEvent.Session(sessionId);

        var userMessage = request.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            yield return ChatStreamEvent.Error("Message cannot be empty.");
            yield break;
        }

        _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "user", Content = userMessage });

        var history = _sessionStore.GetMessages(sessionId);
        var inputItems = ChatResponsesSessionHelper.BuildInputItems(history.Take(history.Count - 1).ToList(), userMessage);

        var client = _settings.CreateResponsesClient();
        var getLatLongTool = ChatToolDefinitions.CreateGetLatLongTool();
        var getLocationTool = ChatToolDefinitions.CreateGetLocationDataTool();
        var getPublicWeatherCurrentTool = ChatToolDefinitions.CreateGetPublicWeatherCurrentTool();
        var getPublicWeatherForecastTool = ChatToolDefinitions.CreateGetPublicWeatherForecastTool();
        var getPublicWeatherHistoryTool = ChatToolDefinitions.CreateGetPublicWeatherHistoryTool();

        var assistantBuilder = new StringBuilder();
        string? errorMessage = null;
        bool requiresAction;

        do
        {
            requiresAction = false;

            CreateResponseOptions options = new(_settings.DeploymentName, inputItems)
            {
                Tools = { getLatLongTool, getLocationTool, getPublicWeatherCurrentTool, getPublicWeatherForecastTool, getPublicWeatherHistoryTool },
                StreamingEnabled = true,
            };

            await foreach (StreamingResponseUpdate update in client.CreateResponseStreamingAsync(options, cancellationToken))
            {
                if (update is StreamingResponseOutputTextDeltaUpdate textDelta && !string.IsNullOrEmpty(textDelta.Delta))
                {
                    assistantBuilder.Append(textDelta.Delta);
                    yield return ChatStreamEvent.Token(textDelta.Delta);
                }

                if (update is StreamingResponseOutputItemDoneUpdate itemDone)
                {
                    inputItems.Add(itemDone.Item);

                    if (itemDone.Item is FunctionCallResponseItem functionCall)
                    {
                        var toolArguments = ChatToolPayload.Format(functionCall.FunctionArguments);
                        yield return ChatStreamEvent.ToolStart(functionCall.FunctionName, toolArguments);

                        string functionOutput;
                        try
                        {
                            functionOutput = await _toolExecutor.ExecuteAsync(functionCall, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Chat1a tool {ToolName} failed", functionCall.FunctionName);
                            errorMessage = ex.Message;
                            break;
                        }

                        inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, functionOutput));
                        yield return ChatStreamEvent.ToolEnd(
                            functionCall.FunctionName,
                            toolArguments,
                            ChatToolPayload.Format(functionOutput));
                        requiresAction = true;
                    }
                }
            }

            if (errorMessage is not null)
            {
                yield return ChatStreamEvent.Error(errorMessage);
                yield break;
            }
        } while (requiresAction);

        var assistantText = assistantBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(assistantText))
        {
            _sessionStore.AppendMessage(sessionId, new Models.ChatMessage { Role = "assistant", Content = assistantText });
        }

        yield return ChatStreamEvent.Done();
    }
}
