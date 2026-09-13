using System.Text.Json;
using Core.Data;

namespace WeatherWorkerDotNet;

/// <summary>
/// HttpContext-backed <see cref="IAgentActivityContextProvider"/>. Lives here (and is duplicated
/// in api-dotnet and mvc-dotnet) rather than in Core, since Core is also referenced by non-web
/// consumers (FoundryConsoleV1-V5, mcp-srv-func-app) that cannot take a hard dependency on
/// IHttpContextAccessor. In practice this app's AgentActivity rows come from Hangfire recurring
/// jobs (RecurringJobScheduler), which have no ambient HttpContext, so GetContext() returns null
/// here -- registered anyway so a future HTTP-triggered path in this app gets it for free.
/// </summary>
public sealed class HttpAgentActivityContextProvider(IHttpContextAccessor httpContextAccessor) : IAgentActivityContextProvider
{
    public string? GetContext()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new
        {
            method = httpContext.Request.Method,
            path = httpContext.Request.Path.Value,
            queryString = httpContext.Request.QueryString.Value,
            traceIdentifier = httpContext.TraceIdentifier,
            remoteIp = httpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent = httpContext.Request.Headers.UserAgent.ToString(),
        });
    }
}
