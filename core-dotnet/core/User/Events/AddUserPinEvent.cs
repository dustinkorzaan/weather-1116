using Core.User.Models;
using CQMediator;

namespace Core.User.Events;

/// <summary>
/// Adds a new pin to the current user.
/// </summary>
public class AddUserPinEvent : IRequest<UserDTO>
{
    public Guid UserId { get; set; } = Core.Data.Domain.UserConstants.AnonymousUserId;

    public required double Latitude { get; set; }

    public required double Longitude { get; set; }

    public required string LocationName { get; set; }
}
