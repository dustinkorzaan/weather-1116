using Core.Agent.Events;
using Core.Data;
using Core.Data.Domain;
using CQMediator;

namespace Core.Agent.Handlers;

public class LogAgentActivityHandler(
    AgentActivityDbContext? dbContext = null,
    IAgentActivityHostProvider? hostProvider = null,
    IAgentActivityContextProvider? contextProvider = null,
    TimeProvider? timeProvider = null) : IRequestHandler<LogAgentActivityEvent, Guid>
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<Guid> Handle(LogAgentActivityEvent request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

        // Core's AddStandardCoreServices() registers this handler in every host that calls it
        // via CQMediator's blanket assembly scan -- including mcp-srv-app-service and
        // mcp-srv-func-app, which have nothing to do with chat/AIWeather and never register
        // AgentActivityDbContext/IAgentActivityHostProvider. Treat that as "logging isn't wired
        // here" rather than throwing, so those hosts' DI container validation (ValidateOnBuild,
        // on by default in Development) doesn't fail just because this handler exists.
        if (dbContext is null || hostProvider is null)
        {
            return id;
        }

        dbContext.AgentActivity.Add(new AgentActivity
        {
            Id = id,
            CorrelationId = request.CorrelationId ?? id,
            TraceId = request.TraceId,
            SessionId = request.SessionId,
            Feature = request.Feature,
            FeatureCategory = request.FeatureCategory,
            Host = hostProvider.Host,
            Context = contextProvider?.GetContext(),
            Direction = request.Direction,
            AgentName = request.AgentName,
            LoopNumber = request.LoopNumber,
            ToolName = request.ToolName,
            CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Content = request.Content,
            Location = request.Location,
            InputTokenCount = request.InputTokenCount,
            CachedTokenCount = request.CachedTokenCount,
            OutputTokenCount = request.OutputTokenCount,
            ReasoningTokenCount = request.ReasoningTokenCount,
            TotalTokenCount = request.TotalTokenCount,
            RuntimeMs = request.RuntimeMs,
            ErrorMessage = request.ErrorMessage,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return id;
    }
}
