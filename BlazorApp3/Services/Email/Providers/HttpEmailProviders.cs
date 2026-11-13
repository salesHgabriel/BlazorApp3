using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace BlazorApp3.Services.Email;

/// <summary>SendGrid Web API v3 (sem SDK). Requer <c>Email:SendGrid:ApiKey</c> e remetente verificado no SendGrid.</summary>
public sealed class SendGridEmailProvider(HttpClient http, IOptions<EmailOptions> options) : IEmailProvider
{
    public string Name => "SendGrid";

    public async Task SendAsync(EmailMessage m, CancellationToken ct = default)
    {
        var o = options.Value.SendGrid;
        if (string.IsNullOrWhiteSpace(o.ApiKey)) throw new InvalidOperationException("Email:SendGrid:ApiKey não configurado.");

        var payload = new
        {
            personalizations = new[] { new { to = new[] { new { email = m.To, name = m.ToName } } } },
            from = new { email = m.From, name = m.FromName },
            subject = m.Subject,
            content = new[]
            {
                new { type = "text/plain", value = m.TextBody ?? HtmlText.ToPlain(m.HtmlBody) },
                new { type = "text/html", value = m.HtmlBody }
            }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, o.Endpoint) { Content = JsonContent.Create(payload) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiKey);
        await HttpEmail.SendAsync(http, request, Name, ct);
    }
}

/// <summary>Mailtrap Email API (envio real ou Sandbox, conforme <c>Email:Mailtrap:Endpoint</c>).</summary>
public sealed class MailtrapEmailProvider(HttpClient http, IOptions<EmailOptions> options) : IEmailProvider
{
    public string Name => "Mailtrap";

    public async Task SendAsync(EmailMessage m, CancellationToken ct = default)
    {
        var o = options.Value.Mailtrap;
        if (string.IsNullOrWhiteSpace(o.ApiToken)) throw new InvalidOperationException("Email:Mailtrap:ApiToken não configurado.");

        var payload = new
        {
            from = new { email = m.From, name = m.FromName },
            to = new[] { new { email = m.To, name = m.ToName } },
            subject = m.Subject,
            html = m.HtmlBody,
            text = m.TextBody ?? HtmlText.ToPlain(m.HtmlBody),
            category = "app"
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, o.Endpoint) { Content = JsonContent.Create(payload) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiToken);
        await HttpEmail.SendAsync(http, request, Name, ct);
    }
}

internal static class HttpEmail
{
    public static async Task SendAsync(HttpClient http, HttpRequestMessage request, string provider, CancellationToken ct)
    {
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"{provider} recusou o e-mail ({(int)response.StatusCode}): {body}", null, response.StatusCode);
        }
    }
}

internal static partial class HtmlText
{
    public static string ToPlain(string html) =>
        System.Net.WebUtility.HtmlDecode(Spaces().Replace(Tags().Replace(html, " "), " ")).Trim();

    [GeneratedRegex("<[^>]+>")] private static partial Regex Tags();
    [GeneratedRegex(@"\s{2,}")] private static partial Regex Spaces();
}
