using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace WeatherMVC.Controllers;

public class Chat1bController : ChatStreamControllerBase
{
    private readonly IChatClientService _chatService;

    public Chat1bController([FromKeyedServices("Chat1b")] IChatClientService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public Task Messages([FromBody] ChatSendMessageRequest request, CancellationToken cancellationToken)
        => StreamChatAsync(_chatService, request, cancellationToken);
}
