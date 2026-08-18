using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WeatherMVC.Controllers;

namespace WeatherMVC.Tests;

public class ForecastControllerTests
{
	[Fact]
	public async Task Get_ReturnsForecast()
	{
		var response = new PublicWeatherForecastResponse { Latitude = 36.1627, Longitude = -86.7816 };
		var mediator = new FakeMediator(response);
		var controller = new ForecastController(mediator);

		var result = await controller.Get(36.1627, -86.7816, PublicWeatherForecastResolution.Hourly, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		Assert.Same(response, ok.Value);
		Assert.Equal(36.1627, mediator.LastLatitude);
		Assert.Equal(-86.7816, mediator.LastLongitude);
		Assert.Equal(PublicWeatherForecastResolution.Hourly, mediator.LastResolution);
	}

	[Fact]
	public async Task Get_DefaultsResolutionToDaily()
	{
		var mediator = new FakeMediator(new PublicWeatherForecastResponse());
		var controller = new ForecastController(mediator);

		await controller.Get(36.1627, -86.7816, cancellationToken: CancellationToken.None);

		Assert.Equal(PublicWeatherForecastResolution.Daily, mediator.LastResolution);
	}

	[Fact]
	public async Task Get_ReturnsBadRequestWhenCoordinatesAreMissing()
	{
		var controller = new ForecastController(new FakeMediator(new PublicWeatherForecastResponse()));

		var result = await controller.Get(null, -86.7816, cancellationToken: CancellationToken.None);

		Assert.IsType<BadRequestResult>(result.Result);
	}

	[Fact]
	public async Task Get_ReturnsBadRequestWhenCoordinatesAreOutOfRange()
	{
		var controller = new ForecastController(new FakeMediator(new PublicWeatherForecastResponse()));

		var result = await controller.Get(91, 0, cancellationToken: CancellationToken.None);

		Assert.IsType<BadRequestResult>(result.Result);
	}

	[Fact]
	public async Task Get_ReturnsBadGatewayWhenHandlerThrows()
	{
		var controller = new ForecastController(new FakeMediator(null, throwInvalidOperation: true));

		var result = await controller.Get(36.1627, -86.7816, cancellationToken: CancellationToken.None);

		var status = Assert.IsType<StatusCodeResult>(result.Result);
		Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
	}

	private sealed class FakeMediator(PublicWeatherForecastResponse? response, bool throwInvalidOperation = false) : IMediator
	{
		public double? LastLatitude { get; private set; }

		public double? LastLongitude { get; private set; }

		public PublicWeatherForecastResolution? LastResolution { get; private set; }

		public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
		{
			if (request is GetPublicWeatherForecastEvent forecastEvent)
			{
				LastLatitude = forecastEvent.Latitude;
				LastLongitude = forecastEvent.Longitude;
				LastResolution = forecastEvent.Resolution;
				if (throwInvalidOperation)
				{
					throw new InvalidOperationException("Non-AI: Weather forecast API returned empty or invalid JSON.");
				}

				return Task.FromResult((TResponse)(object)(response ?? new PublicWeatherForecastResponse()));
			}

			throw new NotSupportedException();
		}

		public Task Send(IRequest request, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();
	}
}
