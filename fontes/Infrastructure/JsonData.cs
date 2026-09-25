using System.Text.Json;

static class JsonData
{
    static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
}
