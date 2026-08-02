using System.ClientModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Core.AIWeather.Events;
using Core.AIWeather.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;

namespace Core.AIWeather.Handlers;

/// <summary>
/// Calls the hosted model directly for current weather (same pattern as Foundry Console V4).
/// MCP tools resolve geo and weather data; this handler does not call geo directly.
/// </summary>
public class GetCurrentAIWeatherHandler : IRequestHandler<GetCurrentAIWeatherEvent, AIWeatherResponse>
{
	private const string DeploymentName = "gpt-5.4-mini";

	private static readonly string DefaultLocation = "Nashville, TN";

	private readonly ILogger<GetCurrentAIWeatherHandler> _logger;

	public GetCurrentAIWeatherHandler(ILogger<GetCurrentAIWeatherHandler> logger)
	{
		_logger = logger;
	}

	public async Task<AIWeatherResponse> Handle(GetCurrentAIWeatherEvent request, CancellationToken cancellationToken)
	{
		var location = string.IsNullOrWhiteSpace(request.Location)
			? DefaultLocation
			: request.Location.Trim();

		var endpoint = new Uri(
			Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL."));

		var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
			?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

		var mcpFunctionUrl = Environment.GetEnvironmentVariable("MCP_FUNCTION_URL")
			?? throw new InvalidOperationException("Missing MCP_FUNCTION_URL.");
		var mcpFunctionKey = Environment.GetEnvironmentVariable("MCP_FUNCTION_KEY")
			?? throw new InvalidOperationException("Missing MCP_FUNCTION_KEY.");

		var mcpAppUrl = Environment.GetEnvironmentVariable("MCP_APP_URL")
			?? throw new InvalidOperationException("Missing MCP_APP_URL.");
		var mcpAppKey = Environment.GetEnvironmentVariable("MCP_APP_KEY")
			?? throw new InvalidOperationException("Missing MCP_APP_KEY.");

		var systemPrompt = """
		# Role & Operational Rules
		You are a dedicated weather assistant.
		Always use U.S. customary units exclusively (Fahrenheit, MPH).
		You have access to 3rd-party Model Context Protocol (MCP) tools for location mapping and real-time public meteorology data.

		# Tool Protocol
		1. When given a location, immediately call your coordinates resolution tool to map the location to latitude and longitude.
		2. Use those resolved coordinates to invoke your weather fetching tool.
		3. You must query these tools whenever real weather data is required to fulfill the request.

		# Constraints
		- Output raw JSON text only.
		- Do not include markdown code block wrapper backticks (e.g., do not wrap in ```json).
		- Do not include any conversational pleasantries, introductory text, explanations, or trailing remarks.
		- Do not ask follow-up questions or offer further assistance.

		# JSON Structure Properties
		- fullSummary: Exactly one sentence capturing current weather metrics (temperature, wind speed, wind direction, and overall conditions).
		- For the location name inside the summary sentence, dynamically evaluate and select the most human-friendly city name. Prefer a clean, recognized city name returned by your geo tool over a raw ZIP code, coordinate pair, or opaque input string provided by the user.
		""";

		var userPrompt = $"What is the current weather in: `{location}`?";

		var aiOutputSchema = BuildAIOutputSchema();

		_logger.LogInformation("AI Weather: OpenAI endpoint {Endpoint}, deployment {Deployment}", endpoint, DeploymentName);
		_logger.LogInformation("AI Weather: System prompt for {Location}: {Prompt}", location, systemPrompt);
		_logger.LogInformation("AI Weather: User prompt for {Location}: {Prompt}", location, userPrompt);
		_logger.LogInformation("AI Weather: Output schema for {Location}: {Schema}", location, aiOutputSchema);

		ResponsesClient client = new(
			credential: new ApiKeyCredential(apiKey),
			options: new OpenAIClientOptions
			{
				Endpoint = endpoint,
			});

		McpTool myMcpFunction = ResponseTool.CreateMcpTool(
			serverLabel: "MyMCPFunction",
			serverUri: new Uri($"{mcpFunctionUrl.TrimEnd('/')}/runtime/webhooks/mcp"),
			headers: new Dictionary<string, string> { ["x-functions-key"] = mcpFunctionKey },
			toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

		McpTool myMcpApp = ResponseTool.CreateMcpTool(
			serverLabel: "MyMCPApp",
			serverUri: new Uri($"{mcpAppUrl.TrimEnd('/')}/mcp"),
			authorizationToken: mcpAppKey,
			toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));

		var inputItems = new List<ResponseItem>
		{
			ResponseItem.CreateSystemMessageItem(systemPrompt),
			// Placing the dynamic query at the absolute end ensures the unchanging system instructions,
			// response schema, and MCP tool schemas form a stable hash that qualifies for Azure OpenAI
			// Prompt Caching (1,024+ token threshold).
			ResponseItem.CreateUserMessageItem(userPrompt),
		};

		CreateResponseOptions options = new(DeploymentName, inputItems)
		{
			Tools = { myMcpFunction, myMcpApp },
			TextOptions = new ResponseTextOptions
			{
				TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
					jsonSchemaFormatName: "ai_weather_response",
					jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(aiOutputSchema)),
					jsonSchemaIsStrict: true),
			},
		};

		ResponseResult response = await client.CreateResponseAsync(options, cancellationToken);

		if (response.Status != ResponseStatus.Completed)
		{
			throw new InvalidOperationException(
				$"Model response did not complete. Status: {response.Status?.ToString() ?? "(none)"}, " +
				$"incomplete reason: {response.IncompleteStatusDetails?.Reason?.ToString() ?? "(none)"}, " +
				$"error: {response.Error?.Message ?? "(none)"}");
		}

		var content = response.GetOutputText();
		var aiWeather = JsonSerializer.Deserialize<AIWeatherResponse>(content);

		if (aiWeather is null)
		{
			throw new InvalidOperationException(
				$"Model returned empty or invalid JSON. Raw output: {(string.IsNullOrWhiteSpace(content) ? "(empty)" : content)}");
		}

		return aiWeather;
	}

	private static string BuildAIOutputSchema()
	{
		var schema = JsonSchemaExporter.GetJsonSchemaAsNode(
			JsonSerializerOptions.Default,
			typeof(AIWeatherResponse),
			new JsonSchemaExporterOptions
			{
				TreatNullObliviousAsNonNullable = true,
				TransformSchemaNode = static (context, schema) =>
				{
					if (schema is JsonObject node && node["properties"] is JsonObject properties)
					{
						node["required"] = new JsonArray(properties.Select(property => (JsonNode)property.Key).ToArray());
						node["additionalProperties"] = false;
					}

					return schema;
				},
			});

		return schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
	}
}
