
static class AppChangelog
{
    // User-visible history lives with localized data, independently of UI code.
    static readonly Dictionary<string, string[]> Entries =
        JsonData.ReadResource<Dictionary<string, string[]>>("changelog.json");
    public static readonly string Text = String.Join("\r\n", Entries["en"]);
    public static readonly string PortugueseText = String.Join("\r\n", Entries["pt-BR"]);
}