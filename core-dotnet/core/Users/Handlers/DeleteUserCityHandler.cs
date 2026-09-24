using Core.Data;
using Core.Data.Domain;
using Core.Users.Events;
using CQMediator;
using Microsoft.EntityFrameworkCore;

namespace Core.Users.Handlers;

public class DeleteUserCityHandler : IRequestHandler<DeleteUserCityEvent>
{
    private readonly WX1116DbContext _dbContext;

    public DeleteUserCityHandler(WX1116DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteUserCityEvent request, CancellationToken cancellationToken)
    {
        var city = await _dbContext.UserCities.FirstOrDefaultAsync(p => p.Id == request.UserCityId, cancellationToken);

        if (city == null)
        {
            throw new InvalidOperationException($"Saved city with id {request.UserCityId} not found.");
        }

        _dbContext.UserCities.Remove(city);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
