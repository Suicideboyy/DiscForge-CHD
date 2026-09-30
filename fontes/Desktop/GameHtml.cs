using System;
using System.Net;

static class GameHtml
{
    static string Escape(string text) => WebUtility.HtmlEncode(text ?? "");

    public static string Render(GameInfo game, byte[] cover, bool ps2 = true)
    {
        string mime = cover != null && cover.Length > 0 && cover[0] == 255 ? "image/jpeg" : "image/png";
        string covers = game.CoverProvider.Length > 0 ? game.CoverProvider
            : ps2 ? "xlenore/ps2-covers" : "xlenore/psx-covers";
        string image = cover == null || cover.Length > 10000000
            ? "<div class='placeholder'>" + (Localization.IsPortuguese ? "Capa indisponível" : "Cover unavailable") + "</div>"
            : "<img alt='" + (Localization.IsPortuguese ? "Capa do jogo" : "Game cover")
                + "' src='data:" + mime + ";base64," + Convert.ToBase64String(cover) + "'>";
        return "<!doctype html><html lang='" + Localization.Language + "'><meta charset='utf-8'>"
            + "<meta http-equiv='Content-Security-Policy' content=\"default-src 'none'; img-src data:; style-src 'unsafe-inline'\">"
            + "<style>body{font:15px 'Segoe UI',sans-serif;background:#fff;color:#1c2a42;margin:0;padding:24px}"
            + "h1{font-size:11px;text-transform:uppercase;letter-spacing:2px;color:#637187;margin-bottom:22px}img{width:100%;max-height:330px;object-fit:contain;border-radius:14px}"
            + ".placeholder{background:#f2f5fa;color:#637187;padding:90px 12px;text-align:center;border-radius:18px}"
            + ".tag{background:#6650c8;color:white;padding:8px 12px;border-radius:14px;display:inline-block}"
            + "p{overflow-wrap:anywhere;line-height:1.5}small{color:#666}</style>"
            + "<h1>" + (Localization.IsPortuguese ? "Biblioteca / jogo atual" : "Library / current game")
            + "</h1>" + image + "<h2>" + Escape(game.Title) + "</h2><div class='tag'>"
            + Escape(game.Serial + " • " + game.Type) + "</div><p>" + Escape(Localization.T(game.Status))
            + "</p><p>" + Escape(game.Detection) + "</p><p>" + Escape(game.Lookup)
            + "</p><p>" + Escape(game.Detail) + "</p><small>" + Escape(game.Source)
            + "<br>" + Escape(game.Image) + "<br>" + Escape(game.DatabaseUrl)
            + "<br>" + (Localization.IsPortuguese ? "Capas: " : "Covers: ") + Escape(covers)
            + "<br>" + Escape(game.CoverUrl) + "</small></html>";
    }
}
