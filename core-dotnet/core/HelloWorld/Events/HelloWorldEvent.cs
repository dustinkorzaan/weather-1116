using Core.HelloWorld.Models;
using MediatR;

namespace Core.HelloWorld.Events;

/// <summary>
/// Sample event used to demonstrate the event/handler pattern for the Core project.
/// </summary>
public class HelloWorldEvent : IRequest<HelloWorldResponse>
{
    public required string Message { get; set; }
}
