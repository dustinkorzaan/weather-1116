using Core.Data;
using Core.User.Events;
using CQMediator;
using Microsoft.EntityFrameworkCore;

namespace Core.User.Handlers;

public class DeleteUserPinHandler : IRequestHandler<DeleteUserPinEvent>
{
    private readonly AgentActivityDbContext _dbContext;

    public DeleteUserPinHandler(AgentActivityDbContext dbContext)
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
