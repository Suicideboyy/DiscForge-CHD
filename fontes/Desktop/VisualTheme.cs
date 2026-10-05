using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>Shared palette, metrics and controls for native pages.</summary>
static class VisualTheme
{
    // UI/UX Pro Max: Bento Box Grid structure with its gaming palette.
    // Keep typography and input/focus states native to WinUI.
    //
    // Every colour below is a semantic token: controls reference the token, never
    // the raw RGB value, so the palette can be retuned in one place. High contrast
    // resolves each token to the matching system colour instead of a custom hue.
    static readonly Windows.UI.ViewManagement.AccessibilitySettings accessibility = new();
    static readonly Windows.UI.ViewManagement.UISettings uiSettings = new();

    /// <summary>Reads the live settings each time; controls sample a token once when
    /// they are built, so a switch repaints on the next rebuild or page change.</summary>
    public static bool HighContrast => accessibility.HighContrast;
    /// <summary>Honours the Windows "animations" accessibility setting.</summary>
    public static bool AnimationsEnabled => uiSettings.AnimationsEnabled;

    static SolidColorBrush SystemBrush(Windows.UI.ViewManagement.UIColorType type)
        => new(uiSettings.GetColorValue(type));

    // Surfaces -----------------------------------------------------------------
    /// <summary>Window backdrop. Transparent when Mica paints behind it.</summary>
    public static SolidColorBrush Canvas => HighContrast
        ? SystemBrush(Windows.UI.ViewManagement.UIColorType.Background) : Brush(15, 15, 35);
    /// <summary>Card fill.</summary>
    public static SolidColorBrush Surface => HighContrast ? Canvas : Brush(30, 28, 53);
    /// <summary>Raised or selected fill.</summary>
    public static SolidColorBrush SurfaceHigh => HighContrast ? Canvas : Brush(48, 39, 77);
    public static SolidColorBrush Outline => HighContrast ? Ink : Brush(63, 61, 86);

    // Text ---------------------------------------------------------------------
    public static SolidColorBrush Ink => HighContrast
        ? SystemBrush(Windows.UI.ViewManagement.UIColorType.Foreground) : Brush(226, 232, 240);
    public static SolidColorBrush Muted => HighContrast ? Ink : Brush(148, 163, 184);

    // Accent and status -------------------------------------------------------
    public static SolidColorBrush Accent => HighContrast ? Ink : Brush(167, 139, 250);
    /// <summary>Solid primary fill.</summary>
    public static SolidColorBrush AccentDeep => HighContrast ? Ink : Brush(124, 58, 237);
    public static SolidColorBrush AccentSoft => HighContrast ? Canvas : Brush(48, 39, 77);
    public static SolidColorBrush Teal => HighContrast ? Ink : Brush(94, 234, 212);
    public static SolidColorBrush Warning => HighContrast ? Ink : Brush(251, 191, 36);
    /// <summary>Solid destructive fill.</summary>
    public static SolidColorBrush DangerFill => HighContrast ? Ink : Brush(82, 31, 49);

    public static SolidColorBrush White => HighContrast ? Ink : Brush(255, 255, 255);
    public static SolidColorBrush Brush(byte r, byte g, byte b) => new(Color.FromArgb(255, r, g, b));

    // Metrics -----------------------------------------------------------------
    public const double GapXS = 4, GapS = 8, GapM = 12, GapL = 16, GapXL = 20;
    public const double ControlSmall = 32, ControlNormal = 40;
    public static CornerRadius Tiny => new(8);
    public static CornerRadius XL => new(16);

    // Button roles ------------------------------------------------------------
    // Only AccentButtonStyle is guaranteed to exist in the merged theme
    // resources; every other role overrides the default button surface.
    //
    // Roles are applied as Styles, never as local values: a local Background
    // outranks the control template's visual states and would silently cancel the
    // hover, pressed and disabled feedback.
    static readonly Dictionary<string, Style> roles = new();

    /// <summary>Builds (and caches) the style for one button role. A null minimum
    /// leaves the caller to size the control, which the inline help chip needs to
    /// keep its own circle. Setting no Template means the default one still applies,
    /// so the template's hover, pressed and disabled states stay in charge.</summary>
    static Style Role(string key, SolidColorBrush fill, SolidColorBrush ink,
        SolidColorBrush border, double stroke, double? minimum = ControlNormal)
    {
        if (roles.TryGetValue(key, out var cached)) return cached;
        var style = new Style { TargetType = typeof(Button) };
        if (minimum.HasValue)
            style.Setters.Add(new Setter { Property = Control.MinHeightProperty, Value = minimum.Value });
        if (fill != null)
            style.Setters.Add(new Setter { Property = Control.BackgroundProperty, Value = fill });
        if (ink != null)
            style.Setters.Add(new Setter { Property = Control.ForegroundProperty, Value = ink });
        if (border != null)
            style.Setters.Add(new Setter { Property = Control.BorderBrushProperty, Value = border });
        style.Setters.Add(new Setter { Property = Control.BorderThicknessProperty, Value = new Thickness(stroke) });
        roles[key] = style;
        return style;
    }

    /// <summary>The single committing action on a surface. Uses the app accent rather
    /// than the system accent style, so the primary button always matches the section
    /// markers and the chart legend in the same card.</summary>
    public static void Primary(Button button)
    {
        if (HighContrast) { button.Style = null; return; }
        button.Style = Role("primary", AccentDeep, White, null, 0);
    }

    /// <summary>Supporting action that is not the main commitment.</summary>
    public static void Secondary(Button button)
    {
        if (HighContrast) { button.Style = null; return; }
        button.Style = Role("secondary", SurfaceHigh, Ink, Outline, 1);
    }

    /// <summary>Low emphasis action: it shares the card surface, so only the label
    /// and border distinguish it. Still keyboard and pointer reachable.</summary>
    public static void Subtle(Button button)
    {
        if (HighContrast) { button.Style = null; return; }
        button.Style = Role("subtle", Surface, Muted, null, 0);
    }

    /// <summary>Irreversible or data-ending action.</summary>
    public static void Destructive(Button button)
    {
        if (HighContrast) { button.Style = null; return; }
        button.Style = Role("destructive", DangerFill, White, null, 0);
    }

    /// <summary>Inline help chip. No minimum height: the caller sizes the circle.</summary>
    public static void Quiet(Button button)
    {
        if (HighContrast) { button.Style = null; return; }
        button.Style = Role("quiet", AccentSoft, Accent, null, 0, minimum: null);
    }

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

    // Type scale: page > section > card > body, with label and metadata below.
    public static TextBlock PageTitle(string value) => Text(value, 20, true);
    public static TextBlock SectionTitle(string value) => Text(value, 16, true);
    /// <summary>Small uppercase heading used above card content.</summary>
    public static TextBlock Label(string value)
    {
        var text = Text(value, 11, true);
        text.Foreground = Muted;
        return text;
    }
    /// <summary>De-emphasised supporting line.</summary>
    public static TextBlock Meta(string value)
    {
        var text = Text(value, 12);
        text.Foreground = Muted;
        return text;
    }

    public static Border Card(UIElement content, int padding = 20) => Card(content, padding, XL);

    public static Border Card(UIElement content, int padding, CornerRadius radius)
    {
        return new Border
        {
            Background = Surface,
            BorderBrush = Outline,
            BorderThickness = new Thickness(1),
            CornerRadius = radius,
            Padding = new Thickness(padding),
            Child = content
        };
    }

    /// <summary>Section heading with an optional discreet step marker chip.</summary>
    public static StackPanel Section(string number, string title)
    {
        var body = new StackPanel { Spacing = 14 };
        var heading = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        if (number.Length > 0)
        {
            var marker = Text(number, 12, true);
            marker.Foreground = Accent;
            heading.Children.Add(new Border
            {
                Background = AccentSoft, CornerRadius = Tiny,
                Padding = new Thickness(9, 6, 9, 6), Child = marker
            });
        }
        var name = SectionTitle(Localization.T(title));
        name.VerticalAlignment = VerticalAlignment.Center;
        heading.Children.Add(name);
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
