using CQMediator;

namespace Core.Users.Events;

/// <summary>
/// Adds a new saved city to the current user.
/// </summary>
public class AddUserCityEvent : IRequest
{
    public Guid UserId { get; set; } = Core.Data.Domain.UserConstants.AnonymousUserId;

    public required double Latitude { get; set; }

    public required double Longitude { get; set; }

    public required string LocationName { get; set; }
}
