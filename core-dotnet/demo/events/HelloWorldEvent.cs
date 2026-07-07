namespace Core.demo.events;

/// <summary>
/// Sample event used to demonstrate the event/handler pattern for the Core project.
/// </summary>
public class HelloWorldEvent
{
    public required string Message { get; set; }
}
