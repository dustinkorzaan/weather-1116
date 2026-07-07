using Core.demo.handlers;
using MediatR;

namespace Core.demo.events;

/// <summary>
/// Sample event used to demonstrate the event/handler pattern for the Core project.
/// </summary>
public class HelloWorldEvent : IRequest<HelloWorldResponse>
{
    public required string Message { get; set; }
}
