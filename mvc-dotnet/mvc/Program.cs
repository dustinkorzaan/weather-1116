using Azure.Monitor.OpenTelemetry.AspNetCore;
using Core;
using Core.About;
using Core.Chat;
using Core.Data;
using Core.Data.Domain;
using Core.Hangfire;
using DotNetEnv;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using CQMediator;

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

// Hangfire client only: this app enqueues jobs onto the shared storage
// (DB_CONNECTION_STRING); the worker is the only app that runs the servers.
// Falls back to in-memory storage locally when no connection string is set.
// Authenticates via this app's user-assigned managed identity (AZURE_CLIENT_ID,
// set by infra/modules/app-service.bicep) instead of a SQL login/password --
// see ManagedIdentitySqlConnectionStringFactory.
var dbConnectionString = ManagedIdentitySqlConnectionStringFactory.Build(
    builder.Configuration["DB_CONNECTION_STRING"],
    builder.Configuration["AZURE_CLIENT_ID"]);
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

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<IAboutClient, AboutClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddStandardCoreServices();
builder.Services.AddWeatherChatClients();

// dbo.AgentActivity logging (Chat1a-Chat4b and Current AI Weather V3/V4/V5). Unlike Hangfire
// above, DB_CONNECTION_STRING is a hard requirement here -- there is no in-memory fallback, so
// this throws at startup if it's missing. API's Program.cs owns applying migrations
// (Database.Migrate()); this app only reads/writes the already-migrated schema.
builder.Services.AddSingleton<IAgentActivityHostProvider>(new AgentActivityHostProvider(AgentActivityHost.Mvc));
builder.Services.AddDbContext<AgentActivityDbContext>(options => options.UseSqlServer(dbConnectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

public partial class Program;
