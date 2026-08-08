using Core.Chat.Chat1a;
using Core.Chat.Models;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class Chat1aController : ChatStreamControllerBase
{
    private readonly Chat1aService _chatService;

    public Chat1aController(Chat1aService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("messages")]
    public Task PostMessage([FromBody] ChatSendMessageRequest request, CancellationToken cancellationToken)
        => StreamChatAsync(_chatService, request, cancellationToken);
}
