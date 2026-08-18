using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class Chat3Controller : ChatStreamControllerBase
{
    private readonly IChatClientService _chatService;

    public Chat3Controller([FromKeyedServices("Chat3")] IChatClientService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("messages")]
    public Task PostMessage([FromBody] ChatSendMessageRequest request, CancellationToken cancellationToken)
        => StreamChatAsync(_chatService, request, cancellationToken);
}
