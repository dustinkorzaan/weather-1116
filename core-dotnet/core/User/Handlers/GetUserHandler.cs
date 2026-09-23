using Core.Data;
using Core.Data.Domain;
using Core.User.Events;
using Core.User.Models;
using CQMediator;
using Microsoft.EntityFrameworkCore;

namespace Core.User.Handlers;

public class GetUserHandler : IRequestHandler<GetUserEvent, UserDTO>
{
    private readonly AgentActivityDbContext _dbContext;

    public GetUserHandler(AgentActivityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserDTO> Handle(GetUserEvent request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.User
            .Include(u => u.UserPins)
            .FirstOrDefaultAsync(u => u.Id == UserConstants.AnonymousUserId, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException("Anonymous user not found.");
        }

        return new UserDTO
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            UserPins = user.UserPins.Select(pin => new UserPinDTO
            {
                Id = pin.Id,
                Latitude = pin.Latitude,
                Longitude = pin.Longitude,
                LocationName = pin.LocationName,
            }).ToList(),
        };
    }
}
