using CQMediator;

namespace Core.User.Events;

/// <summary>
/// Deletes a pin from the current user.
/// </summary>
public class DeleteUserPinEvent : IRequest
{
    public required Guid UserPinId { get; set; }
}
