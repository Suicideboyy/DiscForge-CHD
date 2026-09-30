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
        public bool UseRamExtraction { get; set; }
        public string RamDiskPath { get; set; } = "";
        public string Language { get; set; } = "en";
        public bool AutoDetectSystem { get; set; }
        public bool LegacyCompatibility { get; set; }
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
    ScrollViewer toolSettingsPage;

    void LoadPreferences()
    {
        try
        {
            if (!File.Exists(PreferencesPath)) return;
            var saved = JsonSerializer.Deserialize<SavedPreferences>(File.ReadAllText(PreferencesPath));
            if (saved == null) return;
            Localization.SetLanguage(saved.Language);
            language.SelectedIndex = Localization.IsPortuguese ? 1 : 0;
            autoDetect.IsChecked = saved.AutoDetectSystem;
            legacyCompatibility.IsChecked = saved.LegacyCompatibility;
            input.Text = saved.Input;
            FollowInputFolder();
            chdmanPath.Text = saved.ChdmanPath;
            ramExtraction.IsChecked = saved.UseRamExtraction;
            ramDiskPath.Text = saved.RamDiskPath;
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
                UseRamExtraction = ramExtraction.IsChecked == true,
                RamDiskPath = ramDiskPath.Text.Trim(),
                Language = Localization.Language,
                AutoDetectSystem = autoDetect.IsChecked == true,
                LegacyCompatibility = legacyCompatibility.IsChecked == true
            };
            File.WriteAllText(PreferencesPath, JsonSerializer.Serialize(saved));
        }
        catch (Exception ex) { Append("Preferences: " + ex.Message); }
    }

    // Bundled tools remain the default; custom paths are optional.
    UIElement BuildToolSettings()
    {
        var body = VisualTheme.Section("⚙", "External tools");
        body.Children.Add(VisualTheme.Text(Localization.T(
            "Leave paths blank to use the tools included with this application."), 13));
        body.Children.Add(Field("Language", language,
            Localization.IsPortuguese ? "Idioma da interface. A escolha é salva automaticamente." :
                "Interface language. Your choice is saved automatically."));
        body.Children.Add(Field("chdman.exe", PathRow(chdmanPath, true), OptionHelp.ChdmanPath));
        body.Children.Add(Field("Existing RAM drive", PathRow(ramDiskPath), OptionHelp.RamDisk));
        ToolTipService.SetToolTip(resetDefaults, OptionHelp.Reset);
        body.Children.Add(resetDefaults);
        var page = new StackPanel { Padding = new Thickness(24), Spacing = 16 };
        page.Children.Add(VisualTheme.Card(body));
        toolSettingsPage = new ScrollViewer { Content = page, Background = VisualTheme.Canvas };
        return toolSettingsPage;
    }

    /// <summary>Restore encoding and tool choices while leaving the user's folders intact.</summary>
    void RestoreDefaults()
    {
        platform.SelectedIndex = 0;
        autoDetect.IsChecked = false;
        legacyCompatibility.IsChecked = false;
        threads.Value = MachineInfo.LogicalProcessors;
        cdHunk.Value = 2448;
        dvdHunk.Value = 2048;
        foreach (var choice in cdCodecChoices) choice.IsChecked = true;
        foreach (var choice in dvdCodecChoices) choice.IsChecked = (string)choice.Tag != "huff";
        online.IsChecked = true;
        delete.IsChecked = false;
        chdmanPath.Text = ramDiskPath.Text = "";
        ramExtraction.IsChecked = false;
        RefreshRamAvailability();
        UpdatePlatform();
        SavePreferences();
    }

    void ApplyControlLabels()
    {
        input.PlaceholderText = Localization.T("Folder containing your games");
        output.PlaceholderText = Localization.T("CHD destination");
        chdmanPath.PlaceholderText =
            Localization.T("Automatic (bundled)");
        ramDiskPath.PlaceholderText = Localization.T("Select an existing RAM drive");
        ramExtraction.Content = Localization.T("Use RAM extraction");
        online.Content = Localization.T("Identify games by serial");
        delete.Content = Localization.T("Remove archive after success");
        autoDetect.Content = Localization.T("Auto-detect system");
        legacyCompatibility.Content = Localization.T("Compatibility with older devices");
        advanced.Header = Localization.T("Encoding options");
        aboutChanges.Header = Localization.T("Changelog");
        resetDefaults.Content = Localization.T("Reset defaults");
        if (!running) status.Text = Localization.T("Ready to start");
    }
}
