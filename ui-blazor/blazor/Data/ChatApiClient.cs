using System.Text;
using System.Text.Json;

namespace WeatherBlazor.Data;

public class ChatSendMessageRequest
{
    public string? SessionId { get; set; }

    public required string Message { get; set; }
}

public class ChatStreamEvent
{
    public string Type { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string? Text { get; set; }
    public string? ToolName { get; set; }
    public string? ToolArguments { get; set; }
    public string? ToolResult { get; set; }
    public string? ErrorMessage { get; set; }

    public ChatUsage? Usage { get; set; }
}

public class ChatUsage
{
    public int? InputTokenCount { get; set; }

    public int? CachedTokenCount { get; set; }

    public int? OutputTokenCount { get; set; }

    public int? ReasoningTokenCount { get; set; }

    public int? TotalTokenCount { get; set; }

    public int RuntimeMs { get; set; }
}

public class ChatApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public ChatApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async IAsyncEnumerable<ChatStreamEvent> StreamMessageAsync(
        string chatTab,
        ChatSendMessageRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{chatTab}/messages")
        {
            Content = JsonContent.Create(request),
        };

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        // An SSE frame is any number of field lines terminated by a blank line.
        var dataBuilder = new StringBuilder();

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (line is null || line.Length == 0)
            {
                if (dataBuilder.Length > 0)
                {
                    var streamEvent = JsonSerializer.Deserialize<ChatStreamEvent>(dataBuilder.ToString(), SerializerOptions);
                    dataBuilder.Clear();
                    if (streamEvent is not null)
                    {
                        yield return streamEvent;
                    }
                }

                if (line is null)
                {
                    break;
                }

                continue;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            dataBuilder.Append(line["data:".Length..].Trim());
        }
    }
}
