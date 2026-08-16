using Core.Geo.Events;
using Core.Geo.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WeatherMVC.Controllers;

namespace WeatherMVC.Tests;

public class GeoControllerTests
{
	[Fact]
	public async Task Index_ReturnsFirstRankedMatch()
	{
		var first = new NonAILatLongResponse
		{
			Rank = 1,
			Name = "Nashville",
			State = "Tennessee",
			Country = "United States",
			Latitude = 36.1627,
			Longitude = -86.7816,
		};
		var mediator = new FakeMediator(new NonAILatLongListResponse { Results = { first } });
		var controller = new GeoController(mediator);

		var result = await controller.Index("Nashville, TN", CancellationToken.None);

		var json = Assert.IsType<JsonResult>(result);
		var body = Assert.IsType<NonAILatLongResponse>(json.Value);
		Assert.Equal("Nashville", body.Name);
		Assert.Equal(36.1627, body.Latitude);
		Assert.Equal("Nashville, TN", mediator.LastLocation);
		Assert.Equal(1, mediator.LastCount);
	}

	[Fact]
	public async Task Index_ReturnsBadRequestWhenLocationIsMissing()
	{
		var controller = new GeoController(new FakeMediator(new NonAILatLongListResponse()));

		var result = await controller.Index(" ", CancellationToken.None);

		Assert.IsType<BadRequestResult>(result);
	}

	[Fact]
	public async Task Index_ReturnsNotFoundWhenHandlerThrows()
	{
		var controller = new GeoController(new FakeMediator(null, throwNotFound: true));

		var result = await controller.Index("Nowhere", CancellationToken.None);

		Assert.IsType<NotFoundResult>(result);
	}

	[Fact]
	public async Task GetLocation_ReturnsPlaceLabel()
	{
		var mediator = new FakeMediator(new NonAILocationResponse { Location = "Nashville, Tennessee" });
		var controller = new GeoController(mediator);

		var result = await controller.GetLocation(36.1627, -86.7816, CancellationToken.None);

		var json = Assert.IsType<JsonResult>(result);
		var body = Assert.IsType<NonAILocationResponse>(json.Value);
		Assert.Equal("Nashville, Tennessee", body.Location);
		Assert.Equal(36.1627, mediator.LastLatitude);
		Assert.Equal(-86.7816, mediator.LastLongitude);
	}

	[Fact]
	public async Task GetLocation_ReturnsBadRequestWhenCoordinatesAreMissing()
	{
		var controller = new GeoController(new FakeMediator(new NonAILocationResponse()));

		var result = await controller.GetLocation(36.1627, null, CancellationToken.None);

		Assert.IsType<BadRequestResult>(result);
	}

	[Fact]
	public async Task GetLocation_ReturnsNotFoundWhenHandlerThrows()
	{
		var controller = new GeoController(new FakeMediator(null, throwNotFound: true));

		var result = await controller.GetLocation(0, 0, CancellationToken.None);

		Assert.IsType<NotFoundResult>(result);
	}

	private sealed class FakeMediator(object? response, bool throwNotFound = false) : IMediator
	{
		public string? LastLocation { get; private set; }

		public int LastCount { get; private set; }

		public double? LastLatitude { get; private set; }

		public double? LastLongitude { get; private set; }

		public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
		{
			if (request is GetLatLongEvent geoEvent)
			{
				LastLocation = geoEvent.Location;
				LastCount = geoEvent.Count;
				if (throwNotFound)
				{
					throw new InvalidOperationException($"Non-AI: No results found for '{geoEvent.Location}'.");
				}

				return Task.FromResult((TResponse)(object)(response ?? new NonAILatLongListResponse()));
			}

			if (request is GetLocationEvent locationEvent)
			{
				LastLatitude = locationEvent.Latitude;
				LastLongitude = locationEvent.Longitude;
				if (throwNotFound)
				{
					throw new InvalidOperationException("Nominatim: No location found for the given coordinates.");
				}

				return Task.FromResult((TResponse)(object)(response ?? new NonAILocationResponse()));
			}

			throw new NotSupportedException();
		}

		public Task Send(IRequest request, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();
	}
}
