using Core.about;

namespace Core.Tests.about;

/// <summary>
/// Tests for <see cref="AboutTreeBuilder"/>. Build metadata is read from the
/// BUILD_NUMBER / BUILD_START environment variables, so those tests clear the
/// variables to keep results deterministic regardless of the host environment.
/// </summary>
public class AboutTreeBuilderTests
{
    [Fact]
    public void BuildApiNode_ReturnsHealthyLeafNamedApi()
    {
        var node = AboutTreeBuilder.BuildApiNode();

        Assert.Equal("API", node.Name);
        Assert.True(node.IsHealthy);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void BuildMvcNode_ReturnsHealthyLeafNamedMvc()
    {
        var node = AboutTreeBuilder.BuildMvcNode();

        Assert.Equal("MVC", node.Name);
        Assert.True(node.IsHealthy);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void BuildApiRoot_PutsApiNodeFirstThenDependencies()
    {
        var dependency = AboutTreeBuilder.BuildMcpDotNetNode();

        var root = AboutTreeBuilder.BuildApiRoot(dependency);

        Assert.Equal("API Root", root.Name);
        Assert.Equal(2, root.Children.Count);
        Assert.Equal("API", root.Children[0].Name);
        Assert.Equal("mcp-dotnet", root.Children[1].Name);
        Assert.True(root.IsHealthy);
    }

    [Fact]
    public void BuildApiRoot_WithNoDependencies_HasOnlySelfNode()
    {
        var root = AboutTreeBuilder.BuildApiRoot();

        Assert.Single(root.Children);
        Assert.Equal("API", root.Children[0].Name);
        Assert.True(root.IsHealthy);
    }

    [Fact]
    public void BuildMvcRoot_NestsApiRootAsSecondChild()
    {
        var dependency = AboutTreeBuilder.BuildMcpFunctionNode();

        var root = AboutTreeBuilder.BuildMvcRoot(dependency);

        Assert.Equal("MVC Root", root.Name);
        Assert.Equal(2, root.Children.Count);
        Assert.Equal("MVC", root.Children[0].Name);

        var apiRoot = root.Children[1];
        Assert.Equal("API Root", apiRoot.Name);
        Assert.Equal("API", apiRoot.Children[0].Name);
        Assert.Contains(apiRoot.Children, child => child.Name == "mcp-function");
        Assert.True(root.IsHealthy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildMcpDotNetNode_ReflectsHealthArgument(bool isHealthy)
    {
        var node = AboutTreeBuilder.BuildMcpDotNetNode(isHealthy);

        Assert.Equal("mcp-dotnet", node.Name);
        Assert.Equal(isHealthy, node.IsHealthy);
        Assert.Empty(node.Children);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildMcpFunctionNode_ReflectsHealthArgument(bool isHealthy)
    {
        var node = AboutTreeBuilder.BuildMcpFunctionNode(isHealthy);

        Assert.Equal("mcp-function", node.Name);
        Assert.Equal(isHealthy, node.IsHealthy);
        Assert.Empty(node.Children);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildWorkerDotNetNode_ReflectsHealthArgument(bool isHealthy)
    {
        var node = AboutTreeBuilder.BuildWorkerDotNetNode(isHealthy);

        Assert.Equal("worker-dotnet", node.Name);
        Assert.Equal(isHealthy, node.IsHealthy);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void BuildMcpNodes_DefaultToHealthy()
    {
        Assert.True(AboutTreeBuilder.BuildMcpDotNetNode().IsHealthy);
        Assert.True(AboutTreeBuilder.BuildMcpFunctionNode().IsHealthy);
        Assert.True(AboutTreeBuilder.BuildWorkerDotNetNode().IsHealthy);
    }

    [Fact]
    public void BuildRoot_UnhealthyDependency_MakesRootUnhealthy()
    {
        var unhealthy = AboutTreeBuilder.BuildMcpDotNetNode(isHealthy: false);

        var root = AboutTreeBuilder.BuildApiRoot(unhealthy);

        Assert.False(root.IsHealthy);
    }

    [Fact]
    public void BuildRoot_UnhealthyGrandchild_MakesRootUnhealthy()
    {
        var selfNode = new AboutNode { Name = "self" };
        var childWithSickGrandchild = new AboutNode
        {
            Name = "child",
            Children = new List<AboutNode>
            {
                new() { Name = "grandchild", IsHealthy = false },
            },
        };

        var root = AboutTreeBuilder.BuildRoot("root", selfNode, childWithSickGrandchild);

        Assert.False(root.IsHealthy);
    }

    [Fact]
    public void ComputeAggregateHealth_EmptySequence_IsHealthy()
    {
        Assert.True(AboutTreeBuilder.ComputeAggregateHealth(Array.Empty<AboutNode>()));
    }

    [Fact]
    public void ComputeAggregateHealth_AllHealthyNestedNodes_IsHealthy()
    {
        var nodes = new List<AboutNode>
        {
            new()
            {
                Name = "a",
                Children = new List<AboutNode> { new() { Name = "a1" } },
            },
            new() { Name = "b" },
        };

        Assert.True(AboutTreeBuilder.ComputeAggregateHealth(nodes));
    }

    [Fact]
    public void ComputeAggregateHealth_TopLevelUnhealthy_IsUnhealthy()
    {
        var nodes = new List<AboutNode>
        {
            new() { Name = "a", IsHealthy = false },
        };

        Assert.False(AboutTreeBuilder.ComputeAggregateHealth(nodes));
    }

    [Fact]
    public void BuildRoot_ReadsBuildNumberAndStartFromEnvironment()
    {
        RunWithBuildEnvironment("4242", "2024-01-02T03:04:05Z", () =>
        {
            var root = AboutTreeBuilder.BuildApiRoot();

            Assert.Equal(4242, root.BuildNumber);
            Assert.NotNull(root.BuildStart);
            Assert.Equal(
                new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                root.BuildStart!.Value.ToUniversalTime());
        });
    }

    [Fact]
    public void BuildRoot_MissingBuildEnvironment_LeavesMetadataNull()
    {
        RunWithBuildEnvironment(null, null, () =>
        {
            var root = AboutTreeBuilder.BuildApiRoot();

            Assert.Null(root.BuildNumber);
            Assert.Null(root.BuildStart);
        });
    }

    [Fact]
    public void BuildRoot_InvalidBuildEnvironment_LeavesMetadataNull()
    {
        RunWithBuildEnvironment("not-a-number", "not-a-date", () =>
        {
            var root = AboutTreeBuilder.BuildApiRoot();

            Assert.Null(root.BuildNumber);
            Assert.Null(root.BuildStart);
        });
    }

    private static void RunWithBuildEnvironment(string? buildNumber, string? buildStart, Action action)
    {
        var previousNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");
        var previousStart = Environment.GetEnvironmentVariable("BUILD_START");
        try
        {
            Environment.SetEnvironmentVariable("BUILD_NUMBER", buildNumber);
            Environment.SetEnvironmentVariable("BUILD_START", buildStart);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("BUILD_NUMBER", previousNumber);
            Environment.SetEnvironmentVariable("BUILD_START", previousStart);
        }
    }
}
