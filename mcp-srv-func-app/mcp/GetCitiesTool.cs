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
			"Find the largest cities (by population) within a radius of a latitude and longitude. Returns each city's name, region, country, coordinates, distance in km, and population, largest first. radiusKm defaults to 161 (range 1-1000), minPopulation to 0, and maxCities to 25 (range 0-100); out-of-range values are adjusted, not rejected. The search radius is capped at 100 km (the GeoDB free-tier limit), and the result reports the radius actually used.")]
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
			"Search radius in kilometers (1-1000, default 161). Searches are capped at 100 km, the GeoDB free-tier limit.",
			false)]
		double? radiusKm,
		[McpToolProperty(
			"minPopulation",
			"Only include cities with at least this many people (0 or more, default 0).",
			false)]
		long? minPopulation,
		[McpToolProperty(
			"maxCities",
			"Maximum number of cities to return (0-100, default 25).",
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
