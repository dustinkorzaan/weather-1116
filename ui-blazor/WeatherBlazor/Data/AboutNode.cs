namespace WeatherBlazor.Data;

public class AboutNode
{
    public string? Name { get; set; }
    public string? Message { get; set; }
    public bool IsHealthy { get; set; }
    public string? Version { get; set; }
    public DateTime? BuildStart { get; set; }
    public int? BuildNumber { get; set; }
    public string? BuildBranchName { get; set; }
    public List<AboutNode> Children { get; set; } = [];
}
