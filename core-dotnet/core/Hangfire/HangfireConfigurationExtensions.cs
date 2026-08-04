using Hangfire;
using Hangfire.Common;

namespace Core.Hangfire;

public static class HangfireConfigurationExtensions
{
    public const int DefaultRetryAttempts = 3;

    /// <summary>
    /// Replaces Hangfire's built-in automatic retry filter (10 attempts by default)
    /// with <see cref="DefaultRetryAttempts"/> retries.
    /// </summary>
    public static IGlobalConfiguration UseDefaultAutomaticRetry(this IGlobalConfiguration config)
    {
        GlobalJobFilters.Filters.Remove<AutomaticRetryAttribute>();
        return config.UseFilter(new AutomaticRetryAttribute { Attempts = DefaultRetryAttempts });
    }
}
