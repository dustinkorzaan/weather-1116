using Azure.Monitor.OpenTelemetry.AspNetCore;
using DotNetEnv;
using Microsoft.FluentUI.AspNetCore.Components;
using WeatherBlazor.Data;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Exports traces/metrics/logs to Application Insights via APPLICATIONINSIGHTS_CONNECTION_STRING
// (set by infra/modules/app-service.bicep). UseAzureMonitor() throws at startup if the
// connection string is missing, so it's opt-in -- local dev runs with no App Insights resource.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();
builder.Services.AddFluentUIComponents();

builder.Services.AddHttpClient<WeatherApiClient>(c =>
{
    var url = builder.Configuration["API_DOTNET_URL"]
        ?? throw new InvalidOperationException("API_DOTNET_URL is not set");

    c.BaseAddress = new(url);
});

builder.Services.AddHttpClient<ChatApiClient>(c =>
{
    var url = builder.Configuration["API_DOTNET_URL"]
        ?? throw new InvalidOperationException("API_DOTNET_URL is not set");

    c.BaseAddress = new(url);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapGet("/Geo/GetLocation", async (
    double latitude,
    double longitude,
    WeatherApiClient client,
    CancellationToken cancellationToken) =>
{
    var result = await client.GetLocation(latitude, longitude, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
