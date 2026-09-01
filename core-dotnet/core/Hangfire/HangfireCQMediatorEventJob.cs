using Hangfire;
using CQMediator;
using Microsoft.Extensions.Logging;

namespace Core.Hangfire;

/// <summary>
/// Generic Hangfire entry point that deserializes a CQMediator request and dispatches it.
/// </summary>
public sealed class HangfireCQMediatorEventJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<HangfireCQMediatorEventJob> _logger;

    public HangfireCQMediatorEventJob(IMediator mediator, ILogger<HangfireCQMediatorEventJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [JobDisplayName("{0}")]
    public async Task SendAsync(string eventDisplayName, string eventTypeName, string eventJson, CancellationToken cancellationToken = default)
    {
        var request = HangfireCQMediatorEventSerializer.Deserialize(eventTypeName, eventJson);
        _logger.LogInformation("Dispatching Hangfire CQMediator event {EventType}", request.GetType().Name);
        await _mediator.SendUntyped(request, cancellationToken);
    }
}
