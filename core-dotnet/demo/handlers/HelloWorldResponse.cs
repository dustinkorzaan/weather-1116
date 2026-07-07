namespace Core.demo.handlers;

/// <summary>
/// Response returned by <see cref="HelloWorldHandler"/> after handling a <see cref="Core.demo.events.HelloWorldEvent"/>.
/// </summary>
public class HelloWorldResponse
{
    public required string RequestMessage { get; set; }
    public required string RequestResponse { get; set; }
}
