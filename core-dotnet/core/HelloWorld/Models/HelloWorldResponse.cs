namespace Core.HelloWorld.Models;

/// <summary>
/// Response returned by <see cref="Core.HelloWorld.handlers.HelloWorldHandler"/> after handling a <see cref="Core.HelloWorld.events.HelloWorldEvent"/>.
/// </summary>
public class HelloWorldResponse
{
    public required string RequestMessage { get; set; }
    public required string RequestResponse { get; set; }

    /// <summary>
    /// The UTC timestamp at which the response was generated.
    /// </summary>
    public required DateTime TimestampUtc { get; set; }
}
