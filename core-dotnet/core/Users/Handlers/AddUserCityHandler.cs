using Core.Data;
using Core.Data.Domain;
using Core.Users.Events;
using CQMediator;
using Microsoft.EntityFrameworkCore;

namespace Core.Users.Handlers;

public class AddUserCityHandler : IRequestHandler<AddUserCityEvent>
{
    private readonly WX1116DbContext _dbContext;

    public AddUserCityHandler(WX1116DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(AddUserCityEvent request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserCities)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User with id {request.UserId} not found.");
        }

        var newCity = new UserCity
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationName = request.LocationName,
        };

        user.UserCities.Add(newCity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
