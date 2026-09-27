using System;

static class AppChangelog
{
    // User-visible features and fixes since the first C# version.
    public static readonly string Text = String.Join("\r\n", new[]
    {
        "2.2.0 — 26/09/2026", "",
        "• CPU and disk readings now reflect the application and converters it starts.",
        "• Multi-track PS2 CDs can be converted when their layout can be identified.",
        "• Different game revisions are kept as separate outputs.",
        "• Stop Now cancels the current task and cleans its temporary files.",
        "• The last input folder is remembered; output defaults to its otimizados folder.",
        "• Codecs are chosen with labeled checkboxes, and tool paths have a Settings page.",
        "• PS1 cover art is now supported.", "",
        "2.1.0 — 26/09/2026", "",
        "• Modern interface with cards, collapsible encoding options and help tooltips.",
        "• DiscForge CHD identity, icon and build date appear in the app.",
        "• The game panel adapts to narrow windows.", "",
        "2.0.0 — 25/09/2026", "",
        "• WinUI 3 interface with game details and covers in WebView2.",
        "• SharpCompress extraction with 7-Zip fallback for incompatible archives.",
        "• Portable distribution with the components needed to run.", "",
        "1.3.0 — 21/09/2026", "",
        "• Covers load during identification, with retries after temporary failures.",
        "• Current input timer and live CPU and disk metrics.",
        "• Thread count is filled from the local machine.", "",
        "1.2.0 — 20/09/2026", "",
        "• PS1/PS2 images and archives convert to CHD.",
        "• Folder, platform, codec, hunk and thread settings.",
        "• Serial lookup, game names and PS2 covers.",
        "• Resume skips games already converted; CHDs are verified before optional archive removal."
    });
}
