namespace Core.Data.Domain;

/// <summary>
/// Constants for the User domain.
/// </summary>
public static class UserConstants
{
    /// <summary>
    /// Default anonymous user GUID used for users not explicitly registered.
    /// </summary>
    public static readonly Guid AnonymousUserId = Guid.Empty;
}
