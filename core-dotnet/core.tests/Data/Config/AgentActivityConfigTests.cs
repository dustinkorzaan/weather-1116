using Core.AIWeather.Handlers;
using Core.Chat.Chat1a;
using Core.Chat.Chat1b;
using Core.Chat.Chat2a;
using Core.Chat.Chat2b;
using Core.Chat.Chat3;
using Core.Chat.Chat4a;
using Core.Chat.Chat4b;
using Core.Data.Domain;

namespace Core.Tests.Data.Config;

public class AgentActivityConfigTests
{
    // Every source that produces an AgentActivity.Feature value in production: the three
    // Current AI Weather handlers (nameof) and all seven chat services (typeof(...).Name). If a
    // future rename makes one of these longer than the configured column, this test -- not a
    // SaveChanges truncation failure in production -- should catch it.
    public static IEnumerable<object[]> ProductionFeatureValues =>
    [
        [nameof(GetCurrentAIWeatherV3Handler)],
        [nameof(GetCurrentAIWeatherV4Handler)],
        [nameof(GetCurrentAIWeatherV5Handler)],
        [typeof(Chat1aService).Name],
        [typeof(Chat1bService).Name],
        [typeof(Chat2aService).Name],
        [typeof(Chat2bService).Name],
        [typeof(Chat3Service).Name],
        [typeof(Chat4aService).Name],
        [typeof(Chat4bService).Name],
    ];

    [Theory]
    [MemberData(nameof(ProductionFeatureValues))]
    public void ProductionFeatureValue_FitsWithinConfiguredColumnLength(string feature)
    {
        Assert.True(
            feature.Length <= AgentActivityColumnLengths.Feature,
            $"\"{feature}\" is {feature.Length} chars, which exceeds AgentActivityColumnLengths.Feature " +
            $"({AgentActivityColumnLengths.Feature}). SaveChanges would truncate/fail this Feature value.");
    }
}
