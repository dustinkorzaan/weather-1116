using Core.demo.forecast;
using MediatR;

namespace WeatherWorkerHangfire;

/// <summary>
/// Hangfire job that exercises Core (via MediatR) to generate a sample forecast.
/// Real persistence and downstream side effects (DB, messaging) come later.
/// </summary>
public class WeatherForecastJob
{
	private readonly IMediator _mediator;
	private readonly ILogger<WeatherForecastJob> _logger;

	public WeatherForecastJob(IMediator mediator, ILogger<WeatherForecastJob> logger)
	{
		_mediator = mediator;
		_logger = logger;
	}

	public async Task RunAsync()
	{
		var forecast = await _mediator.Send(new WeatherForecastEvent());
		_logger.LogInformation("Generated {Count} forecast day(s) via Core.", forecast.Length);
	}
}
