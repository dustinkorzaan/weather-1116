using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Hangfire;

/// <summary>
/// Generic Hangfire entry point that deserializes a MediatR request and dispatches it.
/// </summary>
public sealed class HangfireMediatREventJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<HangfireMediatREventJob> _logger;

    public HangfireMediatREventJob(IMediator mediator, ILogger<HangfireMediatREventJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task SendAsync(string eventTypeName, string eventJson, CancellationToken cancellationToken = default)
    {
        var request = HangfireMediatREventSerializer.Deserialize(eventTypeName, eventJson);
        _logger.LogInformation("Dispatching Hangfire MediatR event {EventType}", request.GetType().Name);
        await _mediator.SendUntyped(request, cancellationToken);
    }
}
