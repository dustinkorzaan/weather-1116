namespace Core.Data;

/// <summary>
/// Supplies the Context column's value for the row currently being logged. Defined here (with no
/// ASP.NET Core dependency) because Core is also referenced by non-web consumers -- the
/// FoundryConsoleV1-V5 demos and mcp-srv-func-app -- so it cannot take a hard dependency on
/// IHttpContextAccessor/HttpContext itself. Each web host (Api, Mvc, Worker) registers its own
/// HttpContext-backed implementation; a host that never registers one gets no Context at all
/// (LogAgentActivityHandler treats a missing registration as "no context available").
/// </summary>
public interface IAgentActivityContextProvider
{
    /// <summary>Null when there's no HTTP request to describe (e.g. a Hangfire recurring job).</summary>
    string? GetContext();
}
