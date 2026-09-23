using Azure.Monitor.OpenTelemetry.AspNetCore;
using Core;
using Core.Data;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Exports traces/metrics/logs to Application Insights via APPLICATIONINSIGHTS_CONNECTION_STRING
// (set by infra/modules/container-app.bicep). UseAzureMonitor() throws at startup if the
// connection string is missing, so it's opt-in -- local dev and WebApplicationFactory-based
// tests run with no App Insights resource at all.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
	builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.AddControllers();

// MCP service is stateless with no database. Register a no-op DbContext for handlers that depend on it
// but won't be used by any MCP tools (User handlers are auto-registered but unused here).
builder.Services.AddDbContext<WX1116DbContext>((_, options) =>
{
	// Use SqlServer with no connection string - will fail if actually used, but handlers won't be.
	options.UseSqlServer("Server=(local);");
});

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
