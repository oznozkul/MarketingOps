using System.Net;
using System.Text.RegularExpressions;

namespace MarketingOps.IntegrationTests.Infrastructure;

/// <summary>Ekran formlarını tarayıcı gibi gönderir: sayfadaki antiforgery token'ı ve htmx başlığıyla.</summary>
public static partial class HtmxForm
{
    public static Task<HttpResponseMessage> PostAsync(HttpClient client, string page, string url, params (string Key, string Value)[] fields)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)))
        };
        request.Headers.Add("RequestVerificationToken", Token().Match(page).Groups[1].Value);
        request.Headers.Add("HX-Request", "true");
        return client.SendAsync(request);
    }

    // Razor Türkçe karakterleri HTML varlığı, toast başlığı JSON kaçışıyla yazar.
    public static async Task<string> BodyAsync(HttpResponseMessage response) => WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    public static string Trigger(HttpResponseMessage response) => Regex.Unescape(response.Headers.GetValues("HX-Trigger").Single());

    public static string Redirect(HttpResponseMessage response) => response.Headers.GetValues("HX-Redirect").Single();

    [GeneratedRegex("""name="request-verification-token" content="([^"]+)" """)]
    private static partial Regex Token();
}
