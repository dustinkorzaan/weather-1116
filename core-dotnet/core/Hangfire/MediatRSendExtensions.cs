using System.Reflection;
using MediatR;

namespace Core.Hangfire;

internal static class MediatRSendExtensions
{
    private static readonly MethodInfo GenericSendMethod = typeof(IMediator)
        .GetMethods()
        .Single(method =>
            method.Name == nameof(IMediator.Send)
            && method.IsGenericMethodDefinition
            && method.GetParameters().Length == 2
            && method.GetParameters()[0].ParameterType.IsGenericType
            && method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IRequest<>));

    public static Task SendUntyped(this IMediator mediator, object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        if (request is IRequest voidRequest && !ImplementsGenericIRequest(requestType))
        {
            return mediator.Send(voidRequest, cancellationToken);
        }

        var responseType = GetResponseType(requestType);
        var send = GenericSendMethod.MakeGenericMethod(responseType);
        return (Task)send.Invoke(mediator, [request, cancellationToken])!;
    }

    private static bool ImplementsGenericIRequest(Type requestType)
    {
        return requestType.GetInterfaces().Any(
            @interface => @interface.IsGenericType
                && @interface.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    private static Type GetResponseType(Type requestType)
    {
        var requestInterface = requestType.GetInterfaces()
            .FirstOrDefault(@interface =>
                @interface.IsGenericType
                && @interface.GetGenericTypeDefinition() == typeof(IRequest<>));

        if (requestInterface is null)
        {
            throw new InvalidOperationException($"'{requestType.Name}' is not a MediatR request type.");
        }

        return requestInterface.GetGenericArguments()[0];
    }
}
