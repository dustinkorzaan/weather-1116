namespace Core.about;

/// <summary>
/// Builds the nested About trees shared by the runnable apps. Core itself is a class
/// library (not an HTTP app), so it only ever appears as a nested "Core Root" -> "Core"
/// subtree inside the API's tree.
/// </summary>
public static class AboutTreeBuilder
{
    public static AboutNode BuildCoreNode() => new() { Name = "Core" };

    public static AboutNode BuildCoreRoot() => BuildRoot("Core Root", BuildCoreNode());

    public static AboutNode BuildApiNode() => new() { Name = "API" };

    public static AboutNode BuildApiRoot() => BuildRoot("API Root", BuildApiNode(), BuildCoreRoot());

    public static AboutNode BuildMvcNode() => new() { Name = "MVC" };

    public static AboutNode BuildMvcRoot() => BuildRoot("MVC Root", BuildMvcNode());

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
}
