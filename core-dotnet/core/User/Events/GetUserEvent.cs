using Core.User.Models;
using CQMediator;

namespace Core.User.Events;

/// <summary>
/// Retrieves the current user and their pins. Assumes anonymous user for now.
/// </summary>
public class GetUserEvent : IRequest<UserDTO>
{
}
