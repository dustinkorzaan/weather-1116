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
        var user = await _dbContext.Users
            .Include(u => u.UserCities)
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
            UserCities = user.UserCities.Select(city => new UserCityDTO
            {
                Id = city.Id,
                Latitude = city.Latitude,
                Longitude = city.Longitude,
                LocationName = city.LocationName,
            }).ToList(),
        };
    }
}
