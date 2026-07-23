using Azure.Messaging.ServiceBus;
using Core.config;
using Core.demo.forecast;
using MediatR;
using Microsoft.Extensions.Options;

namespace WeatherWorkerQueue;

/// <summary>
/// Listens to an Azure Service Bus queue and processes messages through Core
/// (via MediatR). Without a configured connection string the worker starts and
/// stays idle so the project runs before the namespace/key are provisioned.
/// </summary>
public class QueueWorker : BackgroundService
{
	private readonly WeatherQueueOptions _options;
	private readonly IServiceProvider _services;
	private readonly ILogger<QueueWorker> _logger;

	private ServiceBusClient? _client;
	private ServiceBusProcessor? _processor;

	public QueueWorker(
		IOptions<WeatherQueueOptions> options,
		IServiceProvider services,
		ILogger<QueueWorker> logger)
	{
		_options = options.Value;
		_services = services;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (string.IsNullOrWhiteSpace(_options.ConnectionString))
		{
			_logger.LogWarning(
				"No queue connection string configured; worker is idle until one is provided.");
			return;
		}

		_client = new ServiceBusClient(_options.ConnectionString);
		_processor = _client.CreateProcessor(_options.RequestQueueName, new ServiceBusProcessorOptions());

		_processor.ProcessMessageAsync += ProcessMessageAsync;
		_processor.ProcessErrorAsync += ProcessErrorAsync;

		_logger.LogInformation("Listening on queue {Queue}.", _options.RequestQueueName);
		await _processor.StartProcessingAsync(stoppingToken);
	}

	private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
	{
		_logger.LogInformation("Received message {MessageId}.", args.Message.MessageId);

		using var scope = _services.CreateScope();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
		var forecast = await mediator.Send(new WeatherForecastEvent());
		_logger.LogInformation("Processed message via Core: {Count} forecast day(s).", forecast.Length);

		await args.CompleteMessageAsync(args.Message);
	}

	private Task ProcessErrorAsync(ProcessErrorEventArgs args)
	{
		_logger.LogError(args.Exception, "Queue error from {Source}.", args.ErrorSource);
		return Task.CompletedTask;
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		if (_processor is not null)
		{
			await _processor.StopProcessingAsync(cancellationToken);
			await _processor.DisposeAsync();
		}

		if (_client is not null)
		{
			await _client.DisposeAsync();
		}

		await base.StopAsync(cancellationToken);
	}
}
