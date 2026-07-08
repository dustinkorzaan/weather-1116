namespace Core.about;

/// <summary>
/// Shared response contract used by every About endpoint (React host, API, MVC).
/// Each node represents either a "root" wrapper or a concrete app/dependency in the health tree.
/// </summary>
public class AboutNode
{
    public required string Name { get; set; }
    public bool IsHealthy { get; set; }
    public string? Version { get; set; }
    public DateTime? BuildStart { get; set; }
    public int? BuildNumber { get; set; }
    public List<AboutNode> Children { get; set; } = new();
}
