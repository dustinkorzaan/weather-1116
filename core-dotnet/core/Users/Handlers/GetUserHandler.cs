using Core.Data;
using Core.Data.Domain;
using Core.Users.Events;
using Core.Users.Models;
using CQMediator;
using Microsoft.EntityFrameworkCore;

namespace Core.Users.Handlers;

public class GetUserHandler : IRequestHandler<GetUserEvent, UserDTO>
{
    private readonly WX1116DbContext _dbContext;

    public GetUserHandler(WX1116DbContext dbContext)
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
