using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class Chat5bController : ChatStreamControllerBase
{
    private readonly IChat5ClientService _chatService;

    public Chat5bController([FromKeyedServices("Chat5b")] IChat5ClientService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("messages")]
    public Task PostMessage([FromBody] Chat5SendMessageRequest request, CancellationToken cancellationToken)
        => StreamChatAsync(_chatService, request, cancellationToken);
}
