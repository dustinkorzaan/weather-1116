using Core.Data;
using Core.Data.Domain;
using Core.User.Events;
using Core.User.Models;
using CQMediator;
using Microsoft.EntityFrameworkCore;

namespace Core.User.Handlers;

public class DeleteUserPinHandler : IRequestHandler<DeleteUserPinEvent, UserDTO>
{
    private readonly AgentActivityDbContext _dbContext;

    public DeleteUserPinHandler(AgentActivityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserDTO> Handle(DeleteUserPinEvent request, CancellationToken cancellationToken)
    {
        var pin = await _dbContext.UserPin.FirstOrDefaultAsync(p => p.Id == request.UserPinId, cancellationToken);

        if (pin == null)
        {
            throw new InvalidOperationException($"Pin with id {request.UserPinId} not found.");
        }

        _dbContext.UserPin.Remove(pin);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = await _dbContext.User
            .Include(u => u.UserPins)
            .FirstOrDefaultAsync(u => u.Id == pin.UserId, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User with id {pin.UserId} not found.");
        }

        return new UserDTO
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            UserPins = user.UserPins.Select(p => new UserPinDTO
            {
                Id = p.Id,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                LocationName = p.LocationName,
            }).ToList(),
        };
    }
}
