using Core.demo.events;
using MediatR;

namespace Core.demo.handlers;

/// <summary>
/// Sample handler used to demonstrate the event/handler pattern for the Core project.
/// </summary>
public class HelloWorldHandler : IRequestHandler<HelloWorldEvent, HelloWorldResponse>
{
    public Task<HelloWorldResponse> Handle(HelloWorldEvent helloWorldEvent, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HelloWorldResponse
        {
            RequestMessage = helloWorldEvent.Message,
            RequestResponse = $"Hello, {helloWorldEvent.Message}!",
            TimestampUtc = DateTime.UtcNow
        });
    }
}
