using Core.About;
using Core.Chat;
using Core.Hangfire;
using Core.HelloWorld.Handlers;
using DotNetEnv;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using MediatR;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Hangfire client only: this app enqueues jobs onto the shared storage
// (DB_CONNECTION_STRING); the worker is the only app that runs the servers.
// Falls back to in-memory storage locally when no connection string is set.
var dbConnectionString = builder.Configuration["DB_CONNECTION_STRING"];
builder.Services.AddHangfire(config =>
{
	config.UseDefaultAutomaticRetry();

	if (string.IsNullOrWhiteSpace(dbConnectionString))
	{
		config.UseMemoryStorage();
	}
	else
	{
		// Explicit non-zero poll interval keeps Hangfire on interval polling
		// (every 60s) rather than the aggressive/continuous mode.
		config.UseSqlServerStorage(dbConnectionString, new SqlServerStorageOptions
		{
			QueuePollInterval = TimeSpan.FromSeconds(60),
		});
	}
});

builder.Services.AddControllers();
builder.Services.AddHttpClient<IAboutClient, AboutClient>(client =>
{
	client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HelloWorldHandler>());
builder.Services.AddMemoryCache();
builder.Services.AddWeatherChatClients();
builder.Services.AddCors(options =>
{
	options.AddPolicy("ReactClient", policy =>
	{
		var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

		if (allowedOrigins is not { Length: > 0 })
		{
			// No explicit origins configured: fall back to the known local UI dev
			// origins instead of allowing any origin. Configure Cors:AllowedOrigins
			// (appsettings or the Cors__AllowedOrigins__N env vars) for other hosts.
			allowedOrigins = new[]
			{
				"http://localhost:3000",
				"http://localhost:8090",
				"http://localhost:8100",
			};
		}

		policy.WithOrigins(allowedOrigins)
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("ReactClient");

// hack, because the default route is not working in codespaces, so redirect to the about endpoint
app.MapGet("/", () => Results.Redirect("/About"));
app.MapControllers();

app.Run();

public partial class Program;
