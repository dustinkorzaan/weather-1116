using Azure.Monitor.OpenTelemetry.AspNetCore;
using Core;
using DotNetEnv;
using ModelContextProtocol.Server;
using OpenTelemetry;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Exports traces/metrics/logs to Application Insights via APPLICATIONINSIGHTS_CONNECTION_STRING
// (set by infra/modules/app-service.bicep). UseAzureMonitor() throws at startup if the
// connection string is missing, so it's opt-in -- local dev and WebApplicationFactory-based
// tests run with no App Insights resource at all.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
	builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.AddControllers();

builder.Services.AddStandardCoreServices();

builder.Services
	.AddMcpServer(options =>
	{
		options.ServerInfo = new()
		{
			Name = "WeatherMcpSrvAppService",
			Version = "1.0.0",
		};
	})
	.WithHttpTransport(options =>
	{
		// Stateless mode is enough for simple tool calls (no sampling/elicitation).
		options.Stateless = true;
	})
	.WithToolsFromAssembly();

var app = builder.Build();

// Shared secret for MCP clients (Foundry project connection, MCP Inspector, etc.).
var mcpSrvAppServiceKey = builder.Configuration["MCP_SRV_APP_SERVICE_KEY"];

// Auth filter: require a valid Bearer token for all /mcp requests.
app.Use(async (context, next) =>
{
	if (context.Request.Path.StartsWithSegments("/mcp"))
	{
		if (string.IsNullOrWhiteSpace(mcpSrvAppServiceKey))
		{
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return;
		}

		var header = context.Request.Headers.Authorization.ToString();
		const string prefix = "Bearer ";
		if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(header[prefix.Length..].Trim(), mcpSrvAppServiceKey, StringComparison.Ordinal))
		{
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return;
		}
	}

	await next();
});

app.MapMcp("/mcp");
app.MapControllers();

app.Run();

public partial class Program;
