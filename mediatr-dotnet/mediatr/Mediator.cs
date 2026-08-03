using System.Collections.Concurrent;

namespace MediatR;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> Wrappers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerWrapper<TResponse>)Wrappers.GetOrAdd(
            request.GetType(),
            static requestType => Activator.CreateInstance(
                typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    public Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (VoidRequestHandlerWrapper)Wrappers.GetOrAdd(
            request.GetType(),
            static requestType => Activator.CreateInstance(
                typeof(VoidRequestHandlerWrapper<>).MakeGenericType(requestType))!);

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }
}

internal abstract class RequestHandlerWrapper<TResponse>
{
    public abstract Task<TResponse> Handle(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Handle(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetService(typeof(IRequestHandler<TRequest, TResponse>))
            ?? throw new InvalidOperationException(
                $"No handler registered for request type '{typeof(TRequest).Name}'.");

        return ((IRequestHandler<TRequest, TResponse>)handler).Handle((TRequest)request, cancellationToken);
    }
}

internal abstract class VoidRequestHandlerWrapper
{
    public abstract Task Handle(
        IRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal sealed class VoidRequestHandlerWrapper<TRequest> : VoidRequestHandlerWrapper
    where TRequest : IRequest
{
    public override Task Handle(
        IRequest request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetService(typeof(IRequestHandler<TRequest>))
            ?? throw new InvalidOperationException(
                $"No handler registered for request type '{typeof(TRequest).Name}'.");

        return ((IRequestHandler<TRequest>)handler).Handle((TRequest)request, cancellationToken);
    }
}
