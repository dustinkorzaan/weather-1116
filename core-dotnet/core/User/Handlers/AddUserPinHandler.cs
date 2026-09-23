using Core.Data;
using Core.Data.Domain;
using Core.User.Events;
using Core.User.Models;
using CQMediator;
using Microsoft.EntityFrameworkCore;

namespace Core.User.Handlers;

public class AddUserPinHandler : IRequestHandler<AddUserPinEvent, UserDTO>
{
    private readonly AgentActivityDbContext _dbContext;

    public AddUserPinHandler(AgentActivityDbContext dbContext)
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
