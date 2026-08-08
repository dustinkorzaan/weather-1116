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
    public string? ErrorMessage { get; set; }
}

public class ChatApiClient
{
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

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var json = line["data:".Length..].Trim();
            if (string.IsNullOrWhiteSpace(json))
            {
                continue;
            }

            var streamEvent = System.Text.Json.JsonSerializer.Deserialize<ChatStreamEvent>(json);
            if (streamEvent is not null)
            {
                yield return streamEvent;
            }

            if (await reader.ReadLineAsync(cancellationToken) is null)
            {
                break;
            }
        }
    }
}
