using CQMediator;

namespace Core.Users.Events;

/// <summary>
/// Deletes a saved city from the current user.
/// </summary>
public class DeleteUserCityEvent : IRequest
{
    public required Guid UserCityId { get; set; }
}
