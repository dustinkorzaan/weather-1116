using System.Net;
using System.Net.Http.Json;
using Core.Users.Events;
using Core.Users.Models;
using CQMediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WeatherMVC.Tests;

public class UserControllerTests(WeatherMvcWebApplicationFactory factory) : IClassFixture<WeatherMvcWebApplicationFactory>
{
    [Fact]
    public async Task GetUser_IsServedAtUserRoute()
    {
        var mediator = new RecordingMediator();
        using var client = CreateClient(mediator);

        var user = await client.GetFromJsonAsync<UserDTO>("/User");

        Assert.NotNull(user);
        Assert.IsType<GetUserEvent>(Assert.Single(mediator.Requests));
    }

    [Fact]
    public async Task AddPin_IsServedAtUserAddPinRoute()
    {
        var mediator = new RecordingMediator();
        using var client = CreateClient(mediator);

        var response = await client.PostAsJsonAsync("/User/AddPin", new
        {
            latitude = 36.1627,
            longitude = -86.7816,
            locationName = "Nashville, Tennessee",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var added = Assert.IsType<AddUserPinEvent>(Assert.Single(mediator.Requests));
        Assert.Equal("Nashville, Tennessee", added.LocationName);
    }

    [Fact]
    public async Task DeletePin_IsServedAtUserDeletePinRoute()
    {
        var mediator = new RecordingMediator();
        using var client = CreateClient(mediator);
        var pinId = Guid.NewGuid();

        var response = await client.DeleteAsync($"/User/DeletePin?userPinId={pinId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var deleted = Assert.IsType<DeleteUserPinEvent>(Assert.Single(mediator.Requests));
        Assert.Equal(pinId, deleted.UserPinId);
    }

    private HttpClient CreateClient(IMediator mediator) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMediator>();
            services.AddSingleton(mediator);
        })).CreateClient();

    private sealed class RecordingMediator : IMediator
    {
        public List<object> Requests { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            object response = new UserDTO
            {
                Id = Guid.Empty,
                FirstName = "",
                LastName = "",
                Email = "",
            };
            return Task.FromResult((TResponse)response);
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }
}
