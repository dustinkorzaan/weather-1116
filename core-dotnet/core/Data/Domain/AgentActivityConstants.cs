namespace Core.Data.Domain;

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
