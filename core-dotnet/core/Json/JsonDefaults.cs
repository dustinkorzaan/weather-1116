using System.Text.Json;

namespace Core.Json;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    public static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };
}
