using Core.Chat.Chat2b;
using Core.Chat.Models;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class Chat2bController : ChatStreamControllerBase
{
    private readonly Chat2bService _chatService;

    public Chat2bController(Chat2bService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("messages")]
    public Task PostMessage([FromBody] ChatSendMessageRequest request, CancellationToken cancellationToken)
        => StreamChatAsync(_chatService, request, cancellationToken);
}
