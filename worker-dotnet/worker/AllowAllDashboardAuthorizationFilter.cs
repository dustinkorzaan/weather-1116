using Hangfire.Dashboard;

namespace WeatherWorkerDotNet;

/// <summary>
/// Opens the Hangfire dashboard to everyone. Hangfire's default filter allows
/// local requests only, which would 403 remote browsers once deployed; this POC
/// site intentionally leaves the dashboard public (no auth).
/// </summary>
public sealed class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
	public bool Authorize(DashboardContext context) => true;
}
