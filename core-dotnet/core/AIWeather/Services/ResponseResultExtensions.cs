using OpenAI.Responses;

namespace Core.AIWeather.Services;

internal static class ResponseResultExtensions
{
    /// <summary>
    /// OpenAI's <see cref="ResponseResult.GetOutputText"/> dereferences
    /// <see cref="ResponseResult.OutputItems"/> without a null check; guard here so a sparse
    /// agent response becomes a clear validation error instead of a NullReferenceException.
    /// </summary>
    public static string? TryGetOutputText(this ResponseResult? response)
    {
        if (response?.OutputItems is null)
        {
            return null;
        }

        return response.GetOutputText();
    }

    public static IEnumerable<ResponseItem> GetOutputItemsOrEmpty(this ResponseResult response) =>
        response.OutputItems ?? [];
}
