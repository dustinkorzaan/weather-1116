namespace Core.AIWeather;

/// <summary>
/// Normalizes Azure AI Foundry project or OpenAI endpoint URLs for OpenAI SDK clients.
/// </summary>
public static class FoundryOpenAiEndpoint
{
	private const string OpenAiPathSuffix = "/openai/v1";
	private const string ProjectsSegment = "/api/projects/";

	/// <summary>
	/// Returns the resource-scoped OpenAI endpoint used by <c>ResponsesClient</c>
	/// (Foundry Console V4 / production AI weather).
	/// </summary>
	public static Uri ResolveForModelDirect(string projectOrEndpointUrl)
	{
		var uri = ParseRequiredUri(projectOrEndpointUrl);
		return new Uri($"{uri.Scheme}://{uri.Authority}{OpenAiPathSuffix}");
	}

	/// <summary>
	/// Returns the project-scoped OpenAI endpoint used by <c>ProjectOpenAIClient</c>
	/// for model deployments (Foundry Console V3).
	/// </summary>
	public static Uri ResolveForProjectOpenAi(string projectOrEndpointUrl)
	{
		var projectRoot = GetProjectRootUri(projectOrEndpointUrl);
		return new Uri($"{projectRoot}{OpenAiPathSuffix}");
	}

	/// <summary>
	/// Returns the hosted-agent OpenAI protocol endpoint used by
	/// <c>ProjectOpenAIClient</c> with <c>ProjectOpenAIClientOptions.AgentName</c>
	/// (Foundry Console V5).
	/// </summary>
	public static Uri ResolveForHostedAgent(string projectOrEndpointUrl, string agentName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

		var projectRoot = GetProjectRootUri(projectOrEndpointUrl);
		return new Uri($"{projectRoot}/agents/{agentName.Trim()}/endpoint/protocols/openai");
	}

	/// <summary>
	/// Back-compat alias for <see cref="ResolveForModelDirect"/>.
	/// </summary>
	public static Uri Resolve(string projectOrEndpointUrl) => ResolveForModelDirect(projectOrEndpointUrl);

	internal static Uri GetProjectRootUri(string projectOrEndpointUrl)
	{
		var uri = ParseRequiredUri(projectOrEndpointUrl);
		var path = uri.AbsolutePath;

		var projectsIndex = path.IndexOf(ProjectsSegment, StringComparison.OrdinalIgnoreCase);
		if (projectsIndex < 0)
		{
			throw new InvalidOperationException(
				$"Expected a Foundry project URL containing '{ProjectsSegment}', but got '{uri}'.");
		}

		var afterProjects = path[(projectsIndex + ProjectsSegment.Length)..];
		var projectIdEnd = afterProjects.IndexOf('/');
		var projectId = projectIdEnd >= 0 ? afterProjects[..projectIdEnd] : afterProjects;

		if (string.IsNullOrWhiteSpace(projectId))
		{
			throw new InvalidOperationException(
				$"Could not parse a project id from Foundry project URL '{uri}'.");
		}

		return new Uri($"{uri.Scheme}://{uri.Authority}{ProjectsSegment}{projectId}");
	}

	private static Uri ParseRequiredUri(string url)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(url);
		return new Uri(url.TrimEnd('/'));
	}
}
