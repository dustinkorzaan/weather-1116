using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WeatherAPI.Controllers;

namespace WeatherAPI.Tests;

public class HistoryControllerTests
{
	[Fact]
	public async Task Get_ReturnsHistory()
	{
		var response = new UIWeatherHistoryResponse { Latitude = 36.1627, Longitude = -86.7816 };
		var mediator = new FakeMediator(response);
		var controller = new HistoryController(mediator);

		var result = await controller.Get(36.1627, -86.7816, PublicWeatherHistoryResolution.Hourly, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.Same(response, ok.Value);
		Assert.Equal(36.1627, mediator.LastLatitude);
		Assert.Equal(-86.7816, mediator.LastLongitude);
		Assert.Equal(PublicWeatherHistoryResolution.Hourly, mediator.LastResolution);
	}

	[Fact]
	public async Task Get_DefaultsResolutionToDaily()
	{
		var mediator = new FakeMediator(new UIWeatherHistoryResponse());
		var controller = new HistoryController(mediator);

		await controller.Get(36.1627, -86.7816, cancellationToken: CancellationToken.None);

		Assert.Equal(PublicWeatherHistoryResolution.Daily, mediator.LastResolution);
	}

	[Fact]
	public async Task Get_ReturnsBadRequestWhenCoordinatesAreMissing()
	{
		var controller = new HistoryController(new FakeMediator(new UIWeatherHistoryResponse()));

		var result = await controller.Get(null, -86.7816, cancellationToken: CancellationToken.None);

		Assert.IsType<BadRequestResult>(result.Result);
	}

	[Fact]
	public async Task Get_ReturnsBadRequestWhenCoordinatesAreOutOfRange()
	{
		var controller = new HistoryController(new FakeMediator(new UIWeatherHistoryResponse()));

		var result = await controller.Get(91, 0, cancellationToken: CancellationToken.None);

		Assert.IsType<BadRequestResult>(result.Result);
	}

	[Fact]
	public async Task Get_ReturnsBadGatewayWhenHandlerThrows()
	{
		var controller = new HistoryController(new FakeMediator(null, throwInvalidOperation: true));

		var result = await controller.Get(36.1627, -86.7816, cancellationToken: CancellationToken.None);

		var status = Assert.IsType<StatusCodeResult>(result.Result);
		Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
	}

	private sealed class FakeMediator(UIWeatherHistoryResponse? response, bool throwInvalidOperation = false) : IMediator
	{
		public double? LastLatitude { get; private set; }

		public double? LastLongitude { get; private set; }

		public PublicWeatherHistoryResolution? LastResolution { get; private set; }

		public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
		{
			if (request is GetUIWeatherHistoryEvent historyEvent)
			{
				LastLatitude = historyEvent.Latitude;
				LastLongitude = historyEvent.Longitude;
				LastResolution = historyEvent.Resolution;
				if (throwInvalidOperation)
				{
					throw new InvalidOperationException("Non-AI: Weather history API returned empty or invalid JSON.");
				}

				return Task.FromResult((TResponse)(object)(response ?? new UIWeatherHistoryResponse()));
			}

			throw new NotSupportedException();
		}

		public Task Send(IRequest request, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();
	}
}
