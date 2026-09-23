using Core.User.Models;
using CQMediator;

namespace Core.User.Events;

/// <summary>
/// Deletes a pin from the current user.
/// </summary>
public class DeleteUserPinEvent : IRequest<UserDTO>
{
    public required Guid UserPinId { get; set; }
}
