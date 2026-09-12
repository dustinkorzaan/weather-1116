namespace Core.Data.Domain;

public static class WeatherActivityDirection
{
    public const string Request = "Request";
    public const string Response = "Response";
}

/// <summary>Matches the original "model direct or agent or multi agent" framing for this repo's chat/AI weather tabs.</summary>
public static class WeatherActivityFeatureCategory
{
    public const string ModelDirect = "ModelDirect";
    public const string Agent = "Agent";
    public const string MultiAgent = "MultiAgent";
}

public static class WeatherActivityHost
{
    public const string Api = "Api";
    public const string Mvc = "Mvc";
}
