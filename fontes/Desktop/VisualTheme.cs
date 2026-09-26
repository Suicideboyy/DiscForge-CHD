using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>Paleta e componentes básicos compartilhados pelas páginas nativas.</summary>
static class VisualTheme
{
    public static SolidColorBrush Canvas => Brush(242, 245, 250);
    public static SolidColorBrush Ink => Brush(28, 42, 66);
    public static SolidColorBrush Muted => Brush(99, 113, 135);
    public static SolidColorBrush Accent => Brush(104, 83, 218);
    public static SolidColorBrush Teal => Brush(0, 132, 125);
    public static SolidColorBrush White => Brush(255, 255, 255);
    public static SolidColorBrush Navy => Brush(23, 36, 60);
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
            Background = White,
            BorderBrush = Brush(223, 230, 240),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(padding),
            Child = content
        };
    }

    public static StackPanel Section(string number, string title)
    {
        var body = new StackPanel { Spacing = 14 };
        var heading = Text(number + "   " + title, 17, true);
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
