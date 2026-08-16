using Core.Geo.Events;
using Core.Geo.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WeatherAPI.Controllers;

namespace WeatherAPI.Tests;

public class GeoControllerTests
{
	[Fact]
	public async Task Get_ReturnsFirstRankedMatch()
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
		var mediator = new FakeMediator(new NonAILatLongListResponse
		{
			Results = { first, new NonAILatLongResponse { Rank = 2, Name = "Nashville", State = "Arkansas" } },
		});
		var controller = new GeoController(mediator);

		var result = await controller.Get("Nashville, TN", CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var body = Assert.IsType<NonAILatLongResponse>(ok.Value);
		Assert.Equal("Nashville", body.Name);
		Assert.Equal("Tennessee", body.State);
		Assert.Equal(36.1627, body.Latitude);
		Assert.Equal(-86.7816, body.Longitude);
		Assert.Equal("Nashville, TN", mediator.LastLocation);
		Assert.Equal(1, mediator.LastCount);
	}

	[Fact]
	public async Task Get_ReturnsBadRequestWhenLocationIsMissing()
	{
		var controller = new GeoController(new FakeMediator(new NonAILatLongListResponse()));

		var result = await controller.Get("  ", CancellationToken.None);

		Assert.IsType<BadRequestResult>(result.Result);
	}

	[Fact]
	public async Task Get_ReturnsNotFoundWhenNoMatches()
	{
		var controller = new GeoController(new FakeMediator(new NonAILatLongListResponse()));

		var result = await controller.Get("UnknownPlace", CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task Get_ReturnsNotFoundWhenHandlerThrows()
	{
		var controller = new GeoController(new FakeMediator(null, throwNotFound: true));

		var result = await controller.Get("Nowhere", CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	private sealed class FakeMediator(NonAILatLongListResponse? response, bool throwNotFound = false) : IMediator
	{
		public string? LastLocation { get; private set; }

		public int LastCount { get; private set; }

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

			throw new NotSupportedException();
		}

		public Task Send(IRequest request, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();
	}
}
