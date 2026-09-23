using Core.Data;
using Core.Data.Domain;
using Core.Users.Events;
using Core.Users.Models;
using CQMediator;
using Microsoft.EntityFrameworkCore;

namespace Core.Users.Handlers;

public class AddUserPinHandler : IRequestHandler<AddUserPinEvent, UserDTO>
{
    private readonly WX1116DbContext _dbContext;

    public AddUserPinHandler(WX1116DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserDTO> Handle(AddUserPinEvent request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.User
            .Include(u => u.UserPins)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User with id {request.UserId} not found.");
        }

        var newPin = new UserPin
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationName = request.LocationName,
        };

        user.UserPins.Add(newPin);
        await _dbContext.SaveChangesAsync(cancellationToken);

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
