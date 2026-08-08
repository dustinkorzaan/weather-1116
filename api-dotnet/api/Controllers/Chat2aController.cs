using Core.Chat.Chat2a;
using Core.Chat.Models;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class Chat2aController : ChatStreamControllerBase
{
    private readonly Chat2aService _chatService;

    public Chat2aController(Chat2aService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("messages")]
    public Task PostMessage([FromBody] ChatSendMessageRequest request, CancellationToken cancellationToken)
        => StreamChatAsync(_chatService, request, cancellationToken);
}
