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

// The user/city MCP tools (GetUser, AddUserCity, DeleteUserCity) read and write dbo.Users/dbo.UserCities.
// API's Program.cs owns applying EF Core migrations (Database.Migrate()); this app only
// reads/writes the already-migrated schema, same as MVC and the worker.
// Authenticates via this app's user-assigned managed identity (AZURE_CLIENT_ID, set by
// infra/modules/container-app.bicep) -- see ManagedIdentitySqlConnectionStringFactory. Without
// DB_CONNECTION_STRING the app still starts (so /Wake and /About answer) with an unusable
// placeholder connection; the user tools then fail per call and /About reports unhealthy.
var dbConnectionString = ManagedIdentitySqlConnectionStringFactory.Build(
	builder.Configuration["DB_CONNECTION_STRING"],
	builder.Configuration["AZURE_CLIENT_ID"]);
builder.Services.AddDbContext<WX1116DbContext>(options =>
	options.UseSqlServer(dbConnectionString ?? "Server=(local);", sql => sql.UseNetTopologySuite()));

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
