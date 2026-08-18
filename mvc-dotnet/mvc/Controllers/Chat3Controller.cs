using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace WeatherMVC.Controllers;

public class Chat3Controller : ChatStreamControllerBase
{
    private readonly IChatClientService _chatService;

    public Chat3Controller([FromKeyedServices("Chat3")] IChatClientService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public Task Messages([FromBody] ChatSendMessageRequest request, CancellationToken cancellationToken)
        => StreamChatAsync(_chatService, request, cancellationToken);
}
