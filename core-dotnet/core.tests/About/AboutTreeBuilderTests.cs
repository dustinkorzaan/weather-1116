using Core.About;

namespace Core.Tests.About;

/// <summary>
/// Tests for <see cref="AboutTreeBuilder"/>. Build metadata is read from the
/// BUILD_NUMBER / BUILD_START / BUILD_BRANCH_NAME environment variables, so those tests clear the
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
        var dependency = AboutTreeBuilder.BuildMcpSrvAppServiceNode();

        var root = AboutTreeBuilder.BuildApiRoot(dependency);

        Assert.Equal("API Root", root.Name);
        Assert.Equal(2, root.Children.Count);
        Assert.Equal("API", root.Children[0].Name);
        Assert.Equal("mcp-srv-app-service", root.Children[1].Name);
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
        var dependency = AboutTreeBuilder.BuildMcpSrvFuncAppNode();

        var root = AboutTreeBuilder.BuildMvcRoot(dependency);

        Assert.Equal("MVC Root", root.Name);
        Assert.Equal(2, root.Children.Count);
        Assert.Equal("MVC", root.Children[0].Name);

        var apiRoot = root.Children[1];
        Assert.Equal("API Root", apiRoot.Name);
        Assert.Equal("API", apiRoot.Children[0].Name);
        Assert.Contains(apiRoot.Children, child => child.Name == "mcp-srv-func-app");
        Assert.True(root.IsHealthy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildMcpSrvAppServiceNode_ReflectsHealthArgument(bool isHealthy)
    {
        var node = AboutTreeBuilder.BuildMcpSrvAppServiceNode(isHealthy);

        Assert.Equal("mcp-srv-app-service", node.Name);
        Assert.Equal(isHealthy, node.IsHealthy);
        Assert.Empty(node.Children);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildMcpSrvFuncAppNode_ReflectsHealthArgument(bool isHealthy)
    {
        var node = AboutTreeBuilder.BuildMcpSrvFuncAppNode(isHealthy);

        Assert.Equal("mcp-srv-func-app", node.Name);
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
    public void BuildWorkerRoot_PutsWorkerAndHangfireNodesUnderRoot()
    {
        var worker = AboutTreeBuilder.BuildWorkerDotNetNode();
        var hangfire = new AboutNode
        {
            Name = "Hangfire",
            PublicMessage = "0 failed, 1 processing, 2 enqueued",
        };

        var root = AboutTreeBuilder.BuildWorkerRoot(worker, hangfire);

        Assert.Equal("Worker Root", root.Name);
        Assert.Equal(["worker-dotnet", "Hangfire"], root.Children.Select(child => child.Name));
        Assert.Equal("0 failed, 1 processing, 2 enqueued", root.Children[1].PublicMessage);
        Assert.True(root.IsHealthy);
    }

    [Fact]
    public void BuildMcpNodes_DefaultToHealthy()
    {
        Assert.True(AboutTreeBuilder.BuildMcpSrvAppServiceNode().IsHealthy);
        Assert.True(AboutTreeBuilder.BuildMcpSrvFuncAppNode().IsHealthy);
        Assert.True(AboutTreeBuilder.BuildWorkerDotNetNode().IsHealthy);
    }

    [Fact]
    public void BuildRoot_UnhealthyDependency_MakesRootUnhealthy()
    {
        var unhealthy = AboutTreeBuilder.BuildMcpSrvAppServiceNode(isHealthy: false);

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
    public void BuildRoot_ReadsBuildBranchNameFromEnvironment()
    {
        RunWithBuildEnvironment("4242", "2024-01-02T03:04:05Z", "feature/my-branch", () =>
        {
            var root = AboutTreeBuilder.BuildApiRoot();

            Assert.Null(root.BuildBranchName);
            Assert.Equal("feature/my-branch", root.Children[0].BuildBranchName);
        });
    }

    [Fact]
    public void BuildRoot_MissingBuildBranchName_LeavesMetadataNull()
    {
        RunWithBuildEnvironment(null, null, null, () =>
        {
            var root = AboutTreeBuilder.BuildApiRoot();

            Assert.Null(root.BuildBranchName);
            Assert.Null(root.Children[0].BuildBranchName);
        });
    }

    [Fact]
    public void BuildRoot_ReadsBuildNumberAndStartFromEnvironment()
    {
        RunWithBuildEnvironment("4242", "2024-01-02T03:04:05Z", null, () =>
        {
            var root = AboutTreeBuilder.BuildApiRoot();

            Assert.Equal(4242, root.Children[0].BuildNumber);
            Assert.NotNull(root.Children[0].BuildStart);
            Assert.Equal(
                new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                root.Children[0].BuildStart!.Value.ToUniversalTime());
            Assert.Null(root.BuildNumber);
            Assert.Null(root.BuildStart);
        });
    }

    [Fact]
    public void BuildRoot_MissingBuildEnvironment_LeavesMetadataNull()
    {
        RunWithBuildEnvironment(null, null, null, () =>
        {
            var root = AboutTreeBuilder.BuildApiRoot();

            Assert.Null(root.BuildNumber);
            Assert.Null(root.BuildStart);
        });
    }

    [Fact]
    public void BuildRoot_InvalidBuildEnvironment_LeavesMetadataNull()
    {
        RunWithBuildEnvironment("not-a-number", "not-a-date", "main", () =>
        {
            var root = AboutTreeBuilder.BuildApiRoot();

            Assert.Null(root.BuildNumber);
            Assert.Null(root.BuildStart);
        });
    }

    private static void RunWithBuildEnvironment(string? buildNumber, string? buildStart, string? buildBranchName, Action action)
    {
        var previousNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");
        var previousStart = Environment.GetEnvironmentVariable("BUILD_START");
        var previousBranch = Environment.GetEnvironmentVariable("BUILD_BRANCH_NAME");
        try
        {
            Environment.SetEnvironmentVariable("BUILD_NUMBER", buildNumber);
            Environment.SetEnvironmentVariable("BUILD_START", buildStart);
            Environment.SetEnvironmentVariable("BUILD_BRANCH_NAME", buildBranchName);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("BUILD_NUMBER", previousNumber);
            Environment.SetEnvironmentVariable("BUILD_START", previousStart);
            Environment.SetEnvironmentVariable("BUILD_BRANCH_NAME", previousBranch);
        }
    }
}
