using System.Reflection;

namespace MediatR;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for request type '{requestType.Name}'.");

        var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))
            ?? throw new InvalidOperationException($"Handler type '{handlerType.Name}' does not implement Handle.");

        var result = handleMethod.Invoke(handler, [request, cancellationToken]);
        if (result is Task<TResponse> task)
        {
            return task;
        }

        throw new InvalidOperationException($"Handler for '{requestType.Name}' did not return Task<{typeof(TResponse).Name}>.");
    }

    public async Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for request type '{requestType.Name}'.");

        var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest>.Handle))
            ?? throw new InvalidOperationException($"Handler type '{handlerType.Name}' does not implement Handle.");

        var result = handleMethod.Invoke(handler, [request, cancellationToken]);
        if (result is Task task)
        {
            await task.ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException($"Handler for '{requestType.Name}' did not return Task.");
    }
}
