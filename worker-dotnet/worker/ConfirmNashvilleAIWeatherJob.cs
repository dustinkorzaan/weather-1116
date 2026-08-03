using Core.AIWeather.Events;
using Hangfire;
using MediatR;

namespace WeatherWorkerDotNet;

/// <summary>
/// Daily Hangfire job that exercises Core AI weather for Nashville, TN.
/// </summary>
public class ConfirmNashvilleAIWeatherJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<ConfirmNashvilleAIWeatherJob> _logger;

    public ConfirmNashvilleAIWeatherJob(IMediator mediator, ILogger<ConfirmNashvilleAIWeatherJob> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Queue("batch-multi")]
    public async Task RunAsync()
    {
        await _mediator.Send(new ConfirmNashvilleAIWeatherEvent());
        _logger.LogInformation("Nashville AI weather probe completed.");
    }
}
