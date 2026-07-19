using Core.weather.Handlers;
using MediatR;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>
	cfg.RegisterServicesFromAssemblyContaining<GetPublicWeatherDataHandler>());

builder.Services
	.AddMcpServer(options =>
	{
		options.ServerInfo = new()
		{
			Name = "WeatherMcpDotNet",
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
// Override with env Mcp__ApiKey or appsettings Mcp:ApiKey.
var mcpApiKey = builder.Configuration["Mcp:ApiKey"]
	?? throw new InvalidOperationException("Mcp:ApiKey is not configured.");

app.Use(async (context, next) =>
{
	if (context.Request.Path.StartsWithSegments("/mcp"))
	{
		var header = context.Request.Headers.Authorization.ToString();
		const string prefix = "Bearer ";
		if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(header[prefix.Length..].Trim(), mcpApiKey, StringComparison.Ordinal))
		{
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return;
		}
	}

	await next();
});

app.MapMcp("/mcp");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", server = "WeatherMcpDotNet" }));

app.Run();
