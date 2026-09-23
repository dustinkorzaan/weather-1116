using Core.Data;
using Core.Data.Domain;
using Core.Users.Events;
using CQMediator;
using Microsoft.EntityFrameworkCore;

namespace Core.Users.Handlers;

public class DeleteUserPinHandler : IRequestHandler<DeleteUserPinEvent>
{
    private readonly WX1116DbContext _dbContext;

    public DeleteUserPinHandler(WX1116DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteUserPinEvent request, CancellationToken cancellationToken)
    {
        var pin = await _dbContext.UserPin.FirstOrDefaultAsync(p => p.Id == request.UserPinId, cancellationToken);

        if (pin == null)
        {
            throw new InvalidOperationException($"Pin with id {request.UserPinId} not found.");
        }

        _dbContext.UserPin.Remove(pin);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
