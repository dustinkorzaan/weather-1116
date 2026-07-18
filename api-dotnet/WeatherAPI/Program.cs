using Core.currentweather;
using Core.demo.handlers;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
	options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HelloWorldHandler>());
builder.Services.AddHttpClient<ICurrentWeatherSource, OpenMeteoCurrentWeatherSource>();
builder.Services.AddCors(options =>
{
	options.AddPolicy("ReactClient", policy =>
	{
		var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

		if (allowedOrigins is { Length: > 0 })
		{
			policy.WithOrigins(allowedOrigins)
				.AllowAnyHeader()
				.AllowAnyMethod();
			return;
		}

		// Sample fallback: allow cross-origin calls when no explicit origins are configured.
		policy.AllowAnyOrigin()
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("ReactClient");

// hack, because the default route is not working in codespaces, so redirect to the weatherforecast endpoint
app.MapGet("/", () => Results.Redirect("/about"));
app.MapControllers();

app.Run();

public partial class Program;
