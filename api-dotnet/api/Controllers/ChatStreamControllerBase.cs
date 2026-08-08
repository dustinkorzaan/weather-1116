using System.Text.Json;
using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

public abstract class ChatStreamControllerBase : ControllerBase
{
    protected static async Task WriteSseEventAsync(HttpResponse response, ChatStreamEvent streamEvent, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(streamEvent);
        await response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }

    protected async Task StreamChatAsync(
        IChatClientService chatService,
        ChatSendMessageRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";

        await foreach (var streamEvent in chatService.SendMessageAsync(request, cancellationToken))
        {
            await WriteSseEventAsync(Response, streamEvent, cancellationToken);
        }
    }
}
