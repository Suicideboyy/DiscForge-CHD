using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

sealed partial class MainWindow
{
    static string PreferencesPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DiscForge CHD", "preferences.json");

    sealed class SavedPreferences
    {
        public string Input { get; set; } = "";
        public string ChdmanPath { get; set; } = "";
        public string SevenZipExePath { get; set; } = "";
        public string SevenZipDllPath { get; set; } = "";
    }

    // Keep output under input unless the user customized it during this session.
    void FollowInputFolder()
    {
        string previous = defaultOutput;
        defaultOutput = string.IsNullOrWhiteSpace(input.Text) ? "" :
            Path.Combine(input.Text.Trim(), "otimizados");
        if (string.IsNullOrWhiteSpace(output.Text) ||
            string.Equals(output.Text, previous, StringComparison.OrdinalIgnoreCase))
            output.Text = defaultOutput;
    }

    string defaultOutput = "";

    void LoadPreferences()
    {
        try
        {
            if (!File.Exists(PreferencesPath)) return;
            var saved = JsonSerializer.Deserialize<SavedPreferences>(File.ReadAllText(PreferencesPath));
            if (saved == null) return;
            input.Text = saved.Input;
            FollowInputFolder();
            chdmanPath.Text = saved.ChdmanPath;
            sevenZipExePath.Text = saved.SevenZipExePath;
            sevenZipDllPath.Text = saved.SevenZipDllPath;
        }
        catch (Exception ex) { Append("Preferences: " + ex.Message); }
    }

    void SavePreferences()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
            var saved = new SavedPreferences
            {
                Input = input.Text.Trim(),
                ChdmanPath = chdmanPath.Text.Trim(),
                SevenZipExePath = sevenZipExePath.Text.Trim(),
                SevenZipDllPath = sevenZipDllPath.Text.Trim()
            };
            File.WriteAllText(PreferencesPath, JsonSerializer.Serialize(saved));
        }
        catch (Exception ex) { Append("Preferences: " + ex.Message); }
    }

    // Bundled tools remain the default; custom paths are optional.
    UIElement BuildToolSettings()
    {
        var body = VisualTheme.Section("⚙", "External tools");
        body.Children.Add(VisualTheme.Text(
            "Leave paths blank to use the tools included with this application.", 13));
        body.Children.Add(Field("chdman.exe", PathRow(chdmanPath, true), OptionHelp.ChdmanPath));
        body.Children.Add(Field("7z.exe", PathRow(sevenZipExePath, true), OptionHelp.SevenZipExePath));
        body.Children.Add(Field("7z.dll", PathRow(sevenZipDllPath, true), OptionHelp.SevenZipDllPath));
        var page = new StackPanel { Padding = new Thickness(24), Spacing = 16 };
        page.Children.Add(VisualTheme.Card(body));
        return new ScrollViewer { Content = page, Background = VisualTheme.Canvas };
    }
}
