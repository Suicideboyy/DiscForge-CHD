using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>Shared palette and controls for native pages.</summary>
static class VisualTheme
{
    // UI/UX Pro Max: Bento Box Grid structure with its gaming palette.
    // Keep typography and input/focus states native to WinUI.
    public static bool HighContrast => new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast;
    static SolidColorBrush SystemBrush(Windows.UI.ViewManagement.UIColorType type)
        => new(new Windows.UI.ViewManagement.UISettings().GetColorValue(type));
    public static SolidColorBrush Canvas => HighContrast
        ? SystemBrush(Windows.UI.ViewManagement.UIColorType.Background) : Brush(15, 15, 35);
    public static SolidColorBrush Surface => HighContrast ? Canvas : Brush(30, 28, 53);
    public static SolidColorBrush Outline => HighContrast ? Ink : Brush(63, 61, 86);
    public static SolidColorBrush Ink => HighContrast
        ? SystemBrush(Windows.UI.ViewManagement.UIColorType.Foreground) : Brush(226, 232, 240);
    public static SolidColorBrush Muted => HighContrast ? Ink : Brush(148, 163, 184);
    public static SolidColorBrush Accent => HighContrast ? Ink : Brush(167, 139, 250);
    public static SolidColorBrush Teal => HighContrast ? Ink : Brush(94, 234, 212);
    public static SolidColorBrush White => HighContrast ? Ink : Brush(255, 255, 255);
    public static SolidColorBrush Navy => Surface;
    public static SolidColorBrush Brush(byte r, byte g, byte b) => new(Color.FromArgb(255, r, g, b));

    public static TextBlock Text(string value, double size = 14, bool strong = false)
    {
        return new TextBlock
        {
            Text = value,
            FontSize = size,
            Foreground = Ink,
            FontWeight = strong ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap
        };
    }

    public static Border Card(UIElement content, int padding = 20)
    {
        return new Border
        {
            Background = Surface,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(padding),
            Child = content
        };
    }

    public static StackPanel Section(string number, string title)
    {
        var body = new StackPanel { Spacing = 14 };
        var heading = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        var marker = Text(number, 12, true);
        marker.Foreground = Accent;
        if (number.Length > 0) heading.Children.Add(new Border
        {
            Background = HighContrast ? Surface : Brush(48, 39, 77), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(9, 6, 9, 6), Child = marker
        });
        heading.Children.Add(new TextBlock
        {
            Text = Localization.T(title), FontSize = 17, Foreground = Ink,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap
        });
        body.Children.Add(heading);
        return body;
    }

    public static Grid Pair(FrameworkElement first, FrameworkElement second)
    {
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        first.HorizontalAlignment = second.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.Children.Add(first);
        Grid.SetColumn(second, 1);
        grid.Children.Add(second);
        return grid;
    }
}
