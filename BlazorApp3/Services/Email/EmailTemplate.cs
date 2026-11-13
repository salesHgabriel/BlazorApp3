using System.Net;
using System.Text;
using BlazorApp3.Data;

namespace BlazorApp3.Services.Email;

/// <summary>Layout HTML único dos e-mails (estilos inline, compatível com clientes de e-mail), com as cores do whitelabel.</summary>
public static class EmailTemplate
{
    public sealed record Content(string Title, IReadOnlyList<string> Paragraphs)
    {
        public string? Code { get; init; }
        public string? ButtonText { get; init; }
        public string? ButtonUrl { get; init; }
        public string? Footnote { get; init; }
    }

    public static string FirstName(string fullName) =>
        fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName;

    public static string Render(BrandingSettings brand, Content c)
    {
        static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        var primary = brand.PrimaryColor;
        var sb = new StringBuilder();
        sb.Append($"""
            <!doctype html><html><body style="margin:0;padding:0;background:#f4f5f9;font-family:Segoe UI,Arial,sans-serif;color:#1f2937">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="padding:32px 12px"><tr><td align="center">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;background:#fff;border-radius:12px;overflow:hidden;border:1px solid #e5e7eb">
            <tr><td style="background:{primary};padding:20px 28px;color:#fff;font-size:18px;font-weight:700">{E(brand.AppName)}</td></tr>
            <tr><td style="padding:28px">
            <h1 style="margin:0 0 16px;font-size:20px;color:#111827">{E(c.Title)}</h1>
            """);
        foreach (var p in c.Paragraphs)
            sb.Append($"""<p style="margin:0 0 14px;font-size:15px;line-height:1.55">{E(p)}</p>""");
        if (c.Code is not null)
            sb.Append($"""<div style="margin:20px 0;text-align:center"><span style="display:inline-block;padding:14px 24px;border-radius:10px;background:#f3f4f6;border:1px dashed {primary};font-size:30px;letter-spacing:10px;font-weight:700;font-family:Consolas,monospace;color:#111827">{E(c.Code)}</span></div>""");
        if (c.ButtonUrl is not null)
            sb.Append($"""<div style="margin:22px 0;text-align:center"><a href="{E(c.ButtonUrl)}" style="display:inline-block;background:{primary};color:#fff;text-decoration:none;padding:12px 22px;border-radius:8px;font-weight:600">{E(c.ButtonText)}</a></div>""");
        if (c.Footnote is not null)
            sb.Append($"""<p style="margin:18px 0 0;font-size:13px;color:#6b7280;line-height:1.5">{E(c.Footnote)}</p>""");
        sb.Append($"""
            </td></tr>
            <tr><td style="padding:16px 28px;border-top:1px solid #f0f1f4;font-size:12px;color:#9ca3af">{E(brand.FooterText)}</td></tr>
            </table></td></tr></table></body></html>
            """);
        return sb.ToString();
    }
}
