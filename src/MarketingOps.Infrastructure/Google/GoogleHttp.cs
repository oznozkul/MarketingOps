using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MarketingOps.Application.Connectors.Google;

namespace MarketingOps.Infrastructure.Google;

/// <summary>Google REST çağrılarının ortak kısmı: yetki başlığı ve hata eşlemesi (401 → yetki hatası, diğerleri → anlaşılır mesaj).</summary>
internal static class GoogleHttp
{
    public static async Task<JsonElement> SendAsync(HttpClient http, HttpMethod method, string url, string? accessToken, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        if (accessToken is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null) request.Content = JsonContent.Create(body);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new GoogleApiException("Google'a ulaşılamadı.", inner: ex);
        }
        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
                return text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone();

            var message = Message(text) ?? response.ReasonPhrase ?? "bilinmeyen hata";
            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new GoogleApiException("Google yetkisi geçersiz. Yeniden bağlanın.", isAuthorizationError: true),
                HttpStatusCode.Forbidden => new GoogleApiException($"Google erişimi reddetti: {message}"),
                HttpStatusCode.TooManyRequests => new GoogleApiException("Google kotası aşıldı; senkron daha sonra yeniden denenecek."),
                _ => new GoogleApiException($"Google isteği başarısız ({(int)response.StatusCode}): {message}")
            };
        }
    }

    private static string? Message(string body)
    {
        try
        {
            var root = JsonDocument.Parse(body).RootElement;
            if (root.TryGetProperty("error", out var error))
                return error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var m) ? m.GetString() : error.ToString();
        }
        catch (JsonException) { }
        return null;
    }

    public static string? String(this JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    public static IEnumerable<JsonElement> Array(this JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray() : [];
}
