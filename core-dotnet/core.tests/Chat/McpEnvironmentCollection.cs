namespace Core.Tests.Chat;

/// <summary>
/// <see cref="ChatMcpToolFactoryTests"/> and <see cref="ChatHostedMcpToolFactoryTests"/> both
/// mutate the same process-wide MCP_SRV_* environment variables via their RunWithMcpEnvironment
/// helper. xUnit runs separate test classes (separate default collections) in parallel, so
/// without this they can race and observe env vars the other class just set or cleared
/// mid-test. Sharing this collection forces both classes to run sequentially instead.
/// </summary>
[CollectionDefinition(Name)]
public class McpEnvironmentCollection
{
    public const string Name = "McpEnvironment";
}
