using System;
using System.Globalization;
using System.Net;

static class GameHtml
{
    static string Escape(string text) => WebUtility.HtmlEncode(text ?? "");
    static string Label(string english, string portuguese)
        => Localization.IsPortuguese ? portuguese : english;
    static string Available(string value) => string.IsNullOrWhiteSpace(value)
        ? Label("Not available", "Não disponível") : value;
    static string Size(long bytes) => bytes > 0
        ? (bytes / 1048576d).ToString("N2", Localization.IsPortuguese
            ? CultureInfo.GetCultureInfo("pt-BR") : CultureInfo.GetCultureInfo("en-US")) + " MiB"
        : Available("");
    static string Fact(string label, string value)
        => "<div><dt>" + Escape(label) + "</dt><dd>" + Escape(Available(value)) + "</dd></div>";

    // Display game facts only; paths and lookup diagnostics remain in the activity log.
    public static string Render(GameInfo game, byte[] cover, bool ps2 = true)
    {
        string mime = cover is { Length: > 0 } && cover[0] == 255 ? "image/jpeg" : "image/png";
        string image = cover is not { Length: > 0 and <= 10000000 }
            ? "<div class='placeholder'>" + Label("Cover unavailable", "Capa indisponível") + "</div>"
            : "<img alt='" + Label("Game cover", "Capa do jogo")
                + "' src='data:" + mime + ";base64," + Convert.ToBase64String(cover) + "'>";
        return "<!doctype html><html lang='" + Localization.Language + "'><meta charset='utf-8'>"
            + "<meta http-equiv='Content-Security-Policy' content=\"default-src 'none'; img-src data:; style-src 'unsafe-inline'\">"
            + "<style>body{font:15px 'Segoe UI',sans-serif;background:#fff;color:#1c2a42;margin:0;padding:24px}"
            + "h1{font-size:11px;text-transform:uppercase;letter-spacing:2px;color:#637187;margin-bottom:22px}"
            + "h2{font-size:21px;line-height:1.4;overflow-wrap:anywhere}"
            + "img{width:100%;max-height:330px;object-fit:contain;border-radius:14px}"
            + ".placeholder{background:#f2f5fa;color:#637187;padding:90px 12px;text-align:center;border-radius:18px}"
            + ".status{background:#ede9ff;color:#5743a4;padding:9px 14px;border-radius:12px;display:inline-block}"
            + "dl{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-top:20px}"
            + "dl div{background:#f2f5fa;padding:12px;border-radius:12px;min-width:0}"
            + "dt{font-size:12px;color:#637187;margin-bottom:5px}dd{margin:0;font-weight:600;overflow-wrap:anywhere}"
            + "@media(max-width:360px){dl{grid-template-columns:1fr}}</style>"
            + "<h1>" + Label("Library / current game", "Biblioteca / jogo atual") + "</h1>"
            + image + "<h2>" + Escape(game.Title) + "</h2><div class='status'>"
            + Escape(Localization.T(game.Status)) + "</div><dl>"
            + Fact(Label("Platform", "Plataforma"), game.System)
            + Fact("Serial", game.Serial)
            + Fact(Label("Disc type", "Tipo de mídia"), game.Type)
            + Fact(Label("Release date", "Data de lançamento"), game.ReleaseDate)
            + Fact(Label("Current format", "Formato atual"), game.CurrentFormat)
            + Fact(Label("Current size", "Tamanho atual"), Size(game.CurrentSize))
            + Fact(Label("Original file format", "Formato do arquivo original"), game.SourceFormat)
            + Fact(Label("Original file size", "Tamanho do arquivo original"), Size(game.SourceSize))
            + "</dl></html>";
    }

    public static string PlainText(GameInfo game)
        => game.Title + "\n" + Available(game.System) + " · " + Available(game.Serial)
            + " · " + Available(game.Type) + "\n" + Localization.T(game.Status)
            + "\n" + Label("Current format: ", "Formato atual: ") + Available(game.CurrentFormat)
            + "\n" + Label("Current size: ", "Tamanho atual: ") + Size(game.CurrentSize)
            + "\n" + Label("Release date: ", "Data de lançamento: ") + Available(game.ReleaseDate);
}
