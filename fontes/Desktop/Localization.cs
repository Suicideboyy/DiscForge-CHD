
/// <summary>Small, explicit UI dictionary; engine messages remain its stable report protocol.</summary>
static class Localization
{
    static readonly Dictionary<string, string> Portuguese =
        JsonData.ReadResource<Dictionary<string, string>>("pt-BR.json");

    public static bool IsPortuguese { get; private set; }
    public static void SetLanguage(string language) => IsPortuguese = language == "pt-BR";
    public static string Language => IsPortuguese ? "pt-BR" : "en";
    public static string T(string english) => IsPortuguese && Portuguese.TryGetValue(english, out var value)
        ? value : english;
}
