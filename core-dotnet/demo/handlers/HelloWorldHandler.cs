using Core.demo.events;

namespace Core.demo.handlers;

/// <summary>
/// Sample handler used to demonstrate the event/handler pattern for the Core project.
/// </summary>
public class HelloWorldHandler
{
    public HelloWorldResponse Handle(HelloWorldEvent helloWorldEvent)
    {
        return new HelloWorldResponse
        {
            RequestMessage = helloWorldEvent.Message,
            RequestResponse = $"Hello, {helloWorldEvent.Message}!"
        };
    }
}
