using DotNetEnv;
using Microsoft.FluentUI.AspNetCore.Components;
using WeatherBlazor.Data;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

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
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
