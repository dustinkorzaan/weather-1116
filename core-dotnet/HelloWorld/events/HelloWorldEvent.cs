using Core.HelloWorld.handlers;
using MediatR;

namespace Core.HelloWorld.events;

/// <summary>
/// Sample event used to demonstrate the event/handler pattern for the Core project.
/// </summary>
public class HelloWorldEvent : IRequest<HelloWorldResponse>
{
    public required string Message { get; set; }
}
