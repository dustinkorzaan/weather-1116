using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.AIWeather.Events;
using Core.AIWeather.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace Core.AIWeather.Handlers;

/// <summary>
/// Streams progressive Foundry Agent updates for current weather (same agent pattern as V4 console).
/// </summary>
public class GetCurrentAIWeatherStreamHandler : IStreamRequestHandler<GetCurrentAIWeatherStreamEvent, AIWeatherStreamUpdate>
{
	private readonly ILogger<GetCurrentAIWeatherStreamHandler> _logger;

	public GetCurrentAIWeatherStreamHandler(ILogger<GetCurrentAIWeatherStreamHandler> logger)
	{
		_logger = logger;
	}

	public async IAsyncEnumerable<AIWeatherStreamUpdate> Handle(
		GetCurrentAIWeatherStreamEvent request,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var context = FoundryAgentWeatherRequestFactory.Create(request.Location);
		_logger.LogInformation("AI Weather stream: starting for {Location}", context.Location);

		yield return Status("Starting AI weather request...");

		var options = FoundryAgentWeatherRequestFactory.CreateOptions(context.UserPrompt, streaming: true);
		ResponseResult? completedResponse = null;

		await foreach (var update in context.ResponseClient.CreateResponseStreamingAsync(options, cancellationToken))
		{
			foreach (var streamUpdate in MapStreamingUpdate(update))
			{
				yield return streamUpdate;
			}

			if (update is StreamingResponseCompletedUpdate completed)
			{
				completedResponse = completed.Response;
			}
		}

		if (completedResponse is null)
		{
			yield return Error("Foundry Agent finished without a completed response.");
			yield break;
		}

		var content = completedResponse.GetOutputText();
		var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(
			content,
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		if (aiWeather is null)
		{
			yield return Error(
				$"Foundry Agent returned empty or invalid JSON. Raw output: {(string.IsNullOrWhiteSpace(content) ? "(empty)" : content)}");
			yield break;
		}

		yield return new AIWeatherStreamUpdate
		{
			Type = "complete",
			Result = aiWeather,
		};
	}

	private static IEnumerable<AIWeatherStreamUpdate> MapStreamingUpdate(StreamingResponseUpdate update)
	{
		switch (update)
		{
			case StreamingResponseCreatedUpdate:
				return [Status("Request accepted by Foundry Agent...")];

			case StreamingResponseQueuedUpdate:
				return [Status("Request queued...")];

			case StreamingResponseInProgressUpdate:
				return [Status("Agent is working...")];

			case StreamingResponseMcpListToolsInProgressUpdate:
				return [Status("Agent is discovering available tools...")];

			case StreamingResponseMcpListToolsCompletedUpdate:
				return [Status("Tool discovery completed.")];

			case StreamingResponseMcpCallInProgressUpdate:
				return [Status("Agent is calling a weather tool...")];

			case StreamingResponseMcpCallCompletedUpdate:
				return [Status("Weather tool call completed.")];

			case StreamingResponseMcpCallFailedUpdate:
				return [Status("A weather tool call failed; agent may retry or continue.")];

			case StreamingResponseOutputItemAddedUpdate { Item: McpToolCallItem mcpCall }:
				return [Status($"Agent requested tool: {mcpCall.ToolName}")];

			case StreamingResponseOutputItemDoneUpdate { Item: McpToolCallItem mcpCall }:
				return [Status($"Tool finished: {mcpCall.ToolName}")];

			case StreamingResponseOutputTextDeltaUpdate textDelta when !string.IsNullOrEmpty(textDelta.Delta):
				return
				[
					new AIWeatherStreamUpdate
					{
						Type = "textDelta",
						Delta = textDelta.Delta,
					},
				];

			case StreamingResponseErrorUpdate errorUpdate:
				return [Error(errorUpdate.Message ?? "Foundry Agent returned an error.")];

			case StreamingResponseFailedUpdate failedUpdate:
				return [Error(failedUpdate.Response?.Status.ToString() ?? "Foundry Agent request failed.")];

			default:
				return [];
		}
	}

	private static AIWeatherStreamUpdate Status(string message) => new()
	{
		Type = "status",
		Message = message,
	};

	private static AIWeatherStreamUpdate Error(string message) => new()
	{
		Type = "error",
		Message = message,
	};
}
