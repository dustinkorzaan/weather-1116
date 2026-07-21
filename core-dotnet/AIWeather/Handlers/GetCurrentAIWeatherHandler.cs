using System.Text.Json;
using Core.AIWeather.Events;
using Core.AIWeather.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.AIWeather.Handlers;

/// <summary>
/// Calls the hosted Microsoft Foundry Agent for current weather (same pattern as Foundry Console V4).
/// The agent uses its configured geo/weather tools; this handler does not call geo directly.
/// </summary>
public class GetCurrentAIWeatherHandler : IRequestHandler<GetCurrentAIWeatherEvent, AIWeatherResponse>
{
	private readonly ILogger<GetCurrentAIWeatherHandler> _logger;

	public GetCurrentAIWeatherHandler(ILogger<GetCurrentAIWeatherHandler> logger)
	{
		_logger = logger;
	}

	public async Task<AIWeatherResponse> Handle(GetCurrentAIWeatherEvent request, CancellationToken cancellationToken)
	{
		var context = FoundryAgentWeatherRequestFactory.Create(request.Location);
		_logger.LogInformation("AI Weather: starting for {Location}", context.Location);
		_logger.LogInformation("AI Weather: User prompt for {Location}: {Prompt}", context.Location, context.UserPrompt);

		var options = FoundryAgentWeatherRequestFactory.CreateOptions(context.UserPrompt, streaming: false);
		ResponseResult response = await context.ResponseClient.CreateResponseAsync(options, cancellationToken);
		var content = response.GetOutputText();
		var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(
			content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		if (aiWeather is null)
		{
			throw new InvalidOperationException(
				$"Foundry Agent returned empty or invalid JSON. Raw output: {(string.IsNullOrWhiteSpace(content) ? "(empty)" : content)}");
		}

		return aiWeather;
	}
}
