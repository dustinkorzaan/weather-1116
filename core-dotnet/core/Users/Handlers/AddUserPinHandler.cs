using Core.Data;
using Core.Data.Domain;
using Core.Users.Events;
using CQMediator;
using Microsoft.EntityFrameworkCore;

namespace Core.Users.Handlers;

public class AddUserPinHandler : IRequestHandler<AddUserPinEvent>
{
    private readonly WX1116DbContext _dbContext;

    public AddUserPinHandler(WX1116DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(AddUserPinEvent request, CancellationToken cancellationToken)
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
    }
}
