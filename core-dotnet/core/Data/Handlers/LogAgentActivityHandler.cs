using Core.Data.Domain;
using Core.Data.Events;
using CQMediator;

namespace Core.Data.Handlers;

public class LogAgentActivityHandler(
    AgentActivityDbContext dbContext,
    IAgentActivityHostProvider hostProvider,
    IAgentActivityContextProvider? contextProvider = null,
    TimeProvider? timeProvider = null) : IRequestHandler<LogAgentActivityEvent, Guid>
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<Guid> Handle(LogAgentActivityEvent request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();

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
