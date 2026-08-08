using Core.Chat.Chat1b;
using Core.Chat.Models;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class Chat1bController : ChatStreamControllerBase
{
    private readonly Chat1bService _chatService;

    public Chat1bController(Chat1bService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("messages")]
    public Task PostMessage([FromBody] ChatSendMessageRequest request, CancellationToken cancellationToken)
        => StreamChatAsync(_chatService, request, cancellationToken);
}
