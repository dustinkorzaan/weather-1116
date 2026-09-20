using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.AspNetCore.Mvc;

namespace WeatherMVC.Controllers;

public abstract class ChatStreamControllerBase : Controller
{
    protected static async Task WriteSseEventAsync(HttpResponse response, ChatStreamEvent streamEvent, CancellationToken cancellationToken)
    {
        var json = ChatStreamEventSerializer.Serialize(streamEvent);
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

        try
        {
            await foreach (var streamEvent in chatService.SendMessageAsync(request, cancellationToken))
            {
                await WriteSseEventAsync(Response, streamEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client disconnected mid-stream.
        }
    }

    // Chat5a/Chat5b only — additive overload bound to IChat5ClientService/Chat5SendMessageRequest
    // so Chat1-4's overload above stays untouched. Reuses the same SSE writer/serializer.
    protected async Task StreamChatAsync(
        IChat5ClientService chatService,
        Chat5SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";

        try
        {
            await foreach (var streamEvent in chatService.SendMessageAsync(request, cancellationToken))
            {
                await WriteSseEventAsync(Response, streamEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client disconnected mid-stream.
        }
    }
}
