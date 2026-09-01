using Core.AIWeather.Events;
using Core.HelloWorld.Events;
using Core.Hangfire;

namespace Core.Tests.Hangfire;

public class HangfireCQMediatorEventSerializerTests
{
    [Fact]
    public void GetDisplayName_ReturnsShortEventTypeName()
    {
        Assert.Equal(
            nameof(ConfirmNashvilleAIWeatherEvent),
            HangfireCQMediatorEventSerializer.GetDisplayName(typeof(ConfirmNashvilleAIWeatherEvent)));
    }

    [Fact]
    public void SerializeAndDeserialize_RoundTripsEventPayload()
    {
        var @event = new GetCurrentAIWeatherV3Event { Location = "Nashville, TN" };
        var typeName = HangfireCQMediatorEventSerializer.GetTypeName(typeof(GetCurrentAIWeatherV3Event));
        var json = HangfireCQMediatorEventSerializer.Serialize(@event);

        var deserialized = HangfireCQMediatorEventSerializer.Deserialize(typeName, json);

        var roundTripped = Assert.IsType<GetCurrentAIWeatherV3Event>(deserialized);
        Assert.Equal("Nashville, TN", roundTripped.Location);
    }

    [Fact]
    public void Deserialize_SupportsParameterlessEvents()
    {
        var typeName = HangfireCQMediatorEventSerializer.GetTypeName(typeof(ConfirmNashvilleAIWeatherEvent));
        var deserialized = HangfireCQMediatorEventSerializer.Deserialize(typeName, "{}");

        Assert.IsType<ConfirmNashvilleAIWeatherEvent>(deserialized);
    }
}

public class CQMediatorEventTypeResolverTests
{
    [Fact]
    public void Resolve_ReturnsConcreteRequestType()
    {
        var typeName = HangfireCQMediatorEventSerializer.GetTypeName(typeof(HelloWorldEvent));

        var resolved = CQMediatorEventTypeResolver.Resolve(typeName);

        Assert.Equal(typeof(HelloWorldEvent), resolved);
    }

    [Fact]
    public void Resolve_ThrowsForNonRequestType()
    {
        var typeName = HangfireCQMediatorEventSerializer.GetTypeName(typeof(string));

        var exception = Assert.Throws<InvalidOperationException>(
            () => CQMediatorEventTypeResolver.Resolve(typeName));

        Assert.Contains("not a CQMediator request", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
