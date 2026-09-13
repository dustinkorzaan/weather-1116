namespace Core.Data.Domain;

/// <summary>
/// Column widths shared between <see cref="Core.Data.Config.AgentActivityConfig"/> (the source of truth for
/// the actual SQL Server column) and tests that verify every production Feature value still fits.
/// </summary>
public static class AgentActivityColumnLengths
{
    /// <summary>
    /// Feature is a raw class name (<c>nameof(GetCurrentAIWeatherV3Handler)</c>,
    /// <c>typeof(Chat1aService).Name</c>), not a hand-picked short label -- 64 leaves headroom
    /// beyond today's longest name (GetCurrentAIWeatherV3/4/5Handler, 28 chars).
    /// </summary>
    public const int Feature = 64;
}

public static class AgentActivityDirection
{
    public const string Request = "Request";
    public const string Response = "Response";
}

/// <summary>Matches the original "model direct or agent or multi agent" framing for this repo's chat/AI weather tabs.</summary>
public static class AgentActivityFeatureCategory
{
    public const string ModelDirect = "ModelDirect";
    public const string Agent = "Agent";
    public const string MultiAgent = "MultiAgent";
}

public static class AgentActivityHost
{
    public const string Api = "Api";
    public const string Mvc = "Mvc";

    /// <summary>worker-dotnet -- the confirm-nashville-ai-weather-v3/v4 Hangfire recurring jobs (RecurringJobScheduler).</summary>
    public const string Worker = "Worker";
}
