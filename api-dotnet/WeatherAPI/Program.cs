using Core.demo.handlers;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HelloWorldHandler>());

var app = builder.Build();

app.UseHttpsRedirection();

// hack, because the default route is not working in codespaces, so redirect to the weatherforecast endpoint
app.MapGet("/", () => Results.Redirect("/weatherforecast"));
app.MapControllers();

app.Run();
