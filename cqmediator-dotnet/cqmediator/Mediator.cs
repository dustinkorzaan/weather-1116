using System.Collections.Concurrent;

namespace CQMediator;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    // Key by request type AND response type so void Send and Send<TResponse>
    // (including covariant TResponse) never share a wrapper entry.
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), object> Wrappers = new();

    private static readonly Type VoidResponseType = typeof(void);

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = (request.GetType(), typeof(TResponse));
        var wrapper = (RequestHandlerWrapper<TResponse>)Wrappers.GetOrAdd(
            key,
            static entry => Activator.CreateInstance(
                typeof(RequestHandlerWrapper<,>).MakeGenericType(entry.RequestType, entry.ResponseType))!);

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    public Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = (request.GetType(), VoidResponseType);
        var wrapper = (VoidRequestHandlerWrapper)Wrappers.GetOrAdd(
            key,
            static entry => Activator.CreateInstance(
                typeof(VoidRequestHandlerWrapper<>).MakeGenericType(entry.RequestType))!);

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
