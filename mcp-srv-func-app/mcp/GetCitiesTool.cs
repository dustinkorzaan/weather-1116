using Core.Geo.Events;
using Core.Geo.Models;
using CQMediator;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace WeatherMcpSrvFuncApp;

/// <summary>
/// MCP tool that lists the largest cities near a lat/long via Core/CQMediator.
/// </summary>
public class GetCitiesTool(IMediator mediator)
{
	[Function(nameof(GetCities))]
	public async Task<NonAICitiesResponse> GetCities(
		[McpToolTrigger(
			"GetCities",
			"Find the largest cities (by population) within a radius of a latitude and longitude. Returns each city's name, region, country, coordinates, distance in km, and population, largest first. radiusKm defaults to 161 (range 1-1000), minPopulation to 0, and maxCities to 25 (range 1-100); out-of-range values are adjusted, not rejected. The result reports the radius actually used; country is the two-letter ISO country code (e.g. US).")]
		ToolInvocationContext context,
		[McpToolProperty(
			"latitude",
			"Latitude in decimal degrees",
			true)]
		double latitude,
		[McpToolProperty(
			"longitude",
			"Longitude in decimal degrees",
			true)]
		double longitude,
		[McpToolProperty(
			"radiusKm",
			"Search radius in kilometers (1-1000, default 161).",
			false)]
		double? radiusKm,
		[McpToolProperty(
			"minPopulation",
			"Only include cities with at least this many people (0 or more, default 0).",
			false)]
		long? minPopulation,
		[McpToolProperty(
			"maxCities",
			"Maximum number of cities to return (1-100, default 25).",
			false)]
		int? maxCities)
	{
		return await mediator.Send(new GetCitiesEvent
		{
			Latitude = latitude,
			Longitude = longitude,
			RadiusKm = radiusKm ?? GetCitiesEvent.DefaultRadiusKm,
			MinPopulation = minPopulation ?? GetCitiesEvent.DefaultMinPopulation,
			MaxCities = maxCities ?? GetCitiesEvent.DefaultMaxCities,
		});
	}
}
