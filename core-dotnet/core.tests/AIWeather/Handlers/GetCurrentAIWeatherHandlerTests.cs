namespace Core.Tests.AIWeather.Handlers;

public class GetCurrentAIWeatherHandlerTests
{
    [Fact]
    public void SystemPrompt_AllowsMarkdownAndIncludesLocationFacts()
    {
        var prompt = File.ReadAllText(FindRepoFile("core-dotnet/core/AIWeather/Handlers/GetCurrentAIWeatherHandler.cs"));

        Assert.Contains("one or two sentences", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("place name", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("latitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("longitude", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("temperature", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wind speed", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wind direction", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("conditions", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GitHub-flavored Markdown", prompt);
        Assert.Contains("also JSON fields", prompt);
        Assert.DoesNotContain("Exactly one sentence", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("AIWeatherSystemInstructions", prompt, StringComparison.Ordinal);
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}");
    }
}
