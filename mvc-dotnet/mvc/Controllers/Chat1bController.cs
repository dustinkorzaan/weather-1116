using Core.Chat.Chat1b;
using Core.Chat.Models;
using Microsoft.AspNetCore.Mvc;

namespace WeatherMVC.Controllers;

public class Chat1bController : ChatStreamControllerBase
{
    private readonly Chat1bService _chatService;

    public Chat1bController(Chat1bService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public Task Messages([FromBody] ChatSendMessageRequest request, CancellationToken cancellationToken)
        => StreamChatAsync(_chatService, request, cancellationToken);
}
