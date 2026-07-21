using System.Globalization;

namespace Core.about;

/// <summary>
/// Builds the nested About trees shared by the runnable apps.
/// </summary>
public static class AboutTreeBuilder
{
    public static AboutNode BuildApiNode() => CreateNode("API");

    public static AboutNode BuildApiRoot() => BuildRoot("API Root", BuildApiNode());

    public static AboutNode BuildMvcNode() => CreateNode("MVC");

    public static AboutNode BuildMvcRoot() => BuildRoot("MVC Root", BuildMvcNode(), BuildApiRoot());

    /// <summary>
    /// Single leaf node for the MCP DotNet host (no children).
    /// </summary>
    public static AboutNode BuildMcpDotNetNode(bool isHealthy = true)
    {
        var node = CreateNode("mcp-dotnet");
        node.IsHealthy = isHealthy;
        return node;
    }

    /// <summary>
    /// Creates a root node whose first child is always <paramref name="selfNode"/>,
    /// followed by any additional dependency nodes, then computes the root's
    /// IsHealthy as the aggregate (logical AND) of every descendant's health.
    /// </summary>
    public static AboutNode BuildRoot(string rootName, AboutNode selfNode, params AboutNode[] otherChildren)
    {
        var children = new List<AboutNode> { selfNode };
        children.AddRange(otherChildren);

        var root = new AboutNode
        {
            Name = rootName,
            Children = children,
            BuildNumber = ResolveBuildNumber(),
            BuildStart = ResolveBuildStart(),
        };
        root.IsHealthy = ComputeAggregateHealth(children);

        return root;
    }

    /// <summary>
    /// Recursively computes whether every node in the given subtrees is healthy.
    /// </summary>
    public static bool ComputeAggregateHealth(IEnumerable<AboutNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!node.IsHealthy || !ComputeAggregateHealth(node.Children))
            {
                return false;
            }
        }

        return true;
    }

    private static AboutNode CreateNode(string name)
        => new()
        {
            Name = name,
            BuildNumber = ResolveBuildNumber(),
            BuildStart = ResolveBuildStart(),
        };

    private static int? ResolveBuildNumber()
    {
        var value = Environment.GetEnvironmentVariable("BUILD_NUMBER");
        return int.TryParse(value, out var number) ? number : null;
    }

    private static DateTime? ResolveBuildStart()
    {
        var value = Environment.GetEnvironmentVariable("BUILD_START");
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var startedAt))
        {
            return startedAt;
        }

        return null;
    }
}
