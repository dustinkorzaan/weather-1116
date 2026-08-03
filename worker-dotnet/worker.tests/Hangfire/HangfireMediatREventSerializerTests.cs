using Core.AIWeather.Events;
using Core.HelloWorld.Events;
using WeatherWorkerDotNet.Hangfire;

namespace WeatherWorkerDotNet.Tests.Hangfire;

public class HangfireMediatREventSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundTripsEventPayload()
    {
        var @event = new GetCurrentAIWeatherEvent { Location = "Nashville, TN" };
        var typeName = HangfireMediatREventSerializer.GetTypeName(typeof(GetCurrentAIWeatherEvent));
        var json = HangfireMediatREventSerializer.Serialize(@event);

        var deserialized = HangfireMediatREventSerializer.Deserialize(typeName, json);

        var roundTripped = Assert.IsType<GetCurrentAIWeatherEvent>(deserialized);
        Assert.Equal("Nashville, TN", roundTripped.Location);
    }

    [Fact]
    public void Deserialize_SupportsParameterlessEvents()
    {
        var typeName = HangfireMediatREventSerializer.GetTypeName(typeof(ConfirmNashvilleAIWeatherEvent));
        var deserialized = HangfireMediatREventSerializer.Deserialize(typeName, "{}");

        Assert.IsType<ConfirmNashvilleAIWeatherEvent>(deserialized);
    }
}

public class MediatREventTypeResolverTests
{
    [Fact]
    public void Resolve_ReturnsConcreteRequestType()
    {
        var typeName = HangfireMediatREventSerializer.GetTypeName(typeof(HelloWorldEvent));

        var resolved = MediatREventTypeResolver.Resolve(typeName);

        Assert.Equal(typeof(HelloWorldEvent), resolved);
    }

    [Fact]
    public void Resolve_ThrowsForNonRequestType()
    {
        var typeName = HangfireMediatREventSerializer.GetTypeName(typeof(string));

        var exception = Assert.Throws<InvalidOperationException>(
            () => MediatREventTypeResolver.Resolve(typeName));

        Assert.Contains("not a MediatR request", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
