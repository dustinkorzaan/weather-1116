using Core;
using DotNetEnv;
using ModelContextProtocol.Server;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

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
var mcpSrvAppServiceKey = builder.Configuration["MCP_SRV_APP_SERVICE_KEY"]?.Trim();

// Auth filter: require the shared secret for all /mcp requests.
// Foundry project connections send the credential name as the HTTP header name.
// The HTTP standard is Authorization; some Foundry connections use Authentication.
app.Use(async (context, next) =>
{
	if (context.Request.Path.StartsWithSegments("/mcp"))
	{
		if (string.IsNullOrWhiteSpace(mcpSrvAppServiceKey)
			|| !HasValidMcpSharedSecret(context.Request, mcpSrvAppServiceKey))
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

static bool HasValidMcpSharedSecret(HttpRequest request, string expectedKey)
{
	string[] headers =
	[
		request.Headers.Authorization.ToString(),
		request.Headers["Authentication"].ToString(),
	];

	foreach (var header in headers)
	{
		if (string.IsNullOrWhiteSpace(header))
		{
			continue;
		}

		var token = header.Trim();
		const string prefix = "Bearer ";
		if (token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			token = token[prefix.Length..].Trim();
		}

		if (string.Equals(token, expectedKey, StringComparison.Ordinal))
		{
			return true;
		}
	}

	return false;
}

public partial class Program;
