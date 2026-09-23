using Core.Users.Models;
using CQMediator;

namespace Core.Users.Events;

/// <summary>
/// Deletes a pin from the current user.
/// </summary>
public class DeleteUserPinEvent : IRequest<UserDTO>
{
    public required Guid UserPinId { get; set; }
}
