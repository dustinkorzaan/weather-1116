namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// Result of AddUserPin/DeleteUserPin -- the same <c>{ "success": true }</c> shape Core's
/// WeatherToolExecutor returns for the local-loop versions of these tools.
/// </summary>
public sealed record UserPinToolResult(bool Success);
