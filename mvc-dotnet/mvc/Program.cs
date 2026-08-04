using Core.About;
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

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<IAboutClient, AboutClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HelloWorldHandler>());

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
