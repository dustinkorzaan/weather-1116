namespace Core.about;

/// <summary>
/// Builds <see cref="AboutNode"/> trees shared by every About endpoint (React host, API, MVC).
/// Enforces the "first child is always the app itself" ordering rule and aggregates
/// root health from descendant health.
/// </summary>
public static class AboutNodeFactory
{
    /// <summary>
    /// Creates a leaf node representing an app or dependency itself.
    /// Version/build metadata is intentionally left null for now (real CI/CD metadata is out of scope).
    /// </summary>
    public static AboutNode CreateSelfNode(string name, bool isHealthy = true)
    {
        return new AboutNode
        {
            Name = name,
            IsHealthy = isHealthy,
            Version = null,
            BuildStart = null,
            BuildNumber = null,
            Children = new List<AboutNode>()
        };
    }

    /// <summary>
    /// Creates a root node whose first child must be the self node for that app,
    /// followed by any dependency subtrees, in deterministic order.
    /// Root health is the aggregate of all descendant health (all true => true).
    /// </summary>
    public static AboutNode CreateRoot(string name, params AboutNode[] children)
    {
        var childList = children.ToList();

        return new AboutNode
        {
            Name = name,
            IsHealthy = childList.All(IsSubtreeHealthy),
            Version = null,
            BuildStart = null,
            BuildNumber = null,
            Children = childList
        };
    }

    /// <summary>
    /// Builds the "Core Root" -> "Core" subtree. Core is a class library (not an HTTP app),
    /// so it is only ever nested inside another app's tree (e.g. API).
    /// </summary>
    public static AboutNode CreateCoreSubtree()
    {
        var coreSelf = CreateSelfNode("Core");
        return CreateRoot("Core Root", coreSelf);
    }

    private static bool IsSubtreeHealthy(AboutNode node)
    {
        return node.IsHealthy && node.Children.All(IsSubtreeHealthy);
    }
}
