
static class JsonData
{
    static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    // Embed language data so portable builds do not depend on editable files.
    public static T ReadResource<T>(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new System.IO.IOException("Missing application resource: " + name);
        return JsonSerializer.Deserialize<T>(stream, Options);
    }
}
