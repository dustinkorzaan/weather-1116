using Core.HelloWorld.events;
using MediatR;

namespace Core.HelloWorld.handlers;

/// <summary>
/// Sample handler used to demonstrate the event/handler pattern for the Core project.
/// </summary>
public class HelloWorldHandler : IRequestHandler<HelloWorldEvent, HelloWorldResponse>
{
    public Task<HelloWorldResponse> Handle(HelloWorldEvent helloWorldEvent, CancellationToken cancellationToken)
    {
        var timestampUtc = DateTime.UtcNow;

        return Task.FromResult(new HelloWorldResponse
        {
            RequestMessage = helloWorldEvent.Message,
            RequestResponse = $"Hello, {helloWorldEvent.Message} on {FormatUtcTimestamp(timestampUtc)} UTC!",
            TimestampUtc = timestampUtc
        });
    }

    private static string FormatUtcTimestamp(DateTime timestampUtc)
    {
        var day = timestampUtc.Day;
        var suffix = GetOrdinalSuffix(day);
        var time = timestampUtc.ToString("h:mm tt").ToLowerInvariant();

        return $"{timestampUtc:MMMM} {day}{suffix} {timestampUtc:yyyy} at {time}";
    }

    private static string GetOrdinalSuffix(int day)
    {
        if (day % 100 == 11 || day % 100 == 12 || day % 100 == 13)
        {
            return "th";
        }

        switch (day % 10)
        {
            case 1:
                return "st";
            case 2:
                return "nd";
            case 3:
                return "rd";
            default:
                return "th";
        }
    }
}
