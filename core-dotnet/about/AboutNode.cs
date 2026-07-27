namespace Core.about;

/// <summary>
/// Shared response contract for About endpoints across all apps (React host, API, MVC).
/// Every About response is a single tree of these nodes, with the first child of any
/// root always being the app's own node, followed by any dependency nodes.
/// </summary>
public class AboutNode
{
    public required string Name { get; set; }
    public string? PublicMessage { get; set; }
    public bool IsHealthy { get; set; } = true;
    public string? Version { get; set; }
    public DateTime? BuildStart { get; set; }
    public int? BuildNumber { get; set; }
    public string? BuildBranchName { get; set; }
    public List<AboutNode> Children { get; set; } = new();
}
