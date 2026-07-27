using Core.weather.Handlers;
using DotNetEnv;
using MediatR;
using ModelContextProtocol.Server;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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
var mcpApiKey = builder.Configuration["MCP_API_KEY"];

// Auth filter: require a valid Bearer token for all /mcp requests.
app.Use(async (context, next) =>
{
	if (context.Request.Path.StartsWithSegments("/mcp"))
	{
		if (string.IsNullOrWhiteSpace(mcpApiKey))
		{
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return;
		}

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
app.MapControllers();

app.Run();
