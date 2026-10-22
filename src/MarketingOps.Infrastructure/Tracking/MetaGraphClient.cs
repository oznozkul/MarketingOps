using System.Net.Http.Headers;
using System.Text.Json;
using MarketingOps.Application.Tracking;
using MarketingOps.Domain.Rules.Definitions;

namespace MarketingOps.Infrastructure.Tracking;

/// <summary>
/// Pixel varlığını ve event kaynağını okur. event_id tekilleştirmesi Graph yanıtında kanıtlanamadığı için boş döner.
/// </summary>
public sealed class MetaGraphClient(HttpClient http) : IMetaGraphClient
{
    public async Task<MetaSignals> ReadAsync(string accessToken, string pixelId, CancellationToken cancellationToken)
    {
        if (pixelId.Length is < 10 or > 20 || !pixelId.All(char.IsDigit))
            return new MetaSignals(false, null, null);

        using var pixel = await Send($"{pixelId}?fields=id", accessToken, cancellationToken);
        if (pixel.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            throw new MetaGraphAuthException("Meta token'ı bu pixel'i okuyamadı.");
        if (!pixel.IsSuccessStatusCode) return new MetaSignals(false, null, null);

        bool? capi = null;
        using var stats = await Send($"{pixelId}/stats?aggregation=event_source", accessToken, cancellationToken);
        if (stats.IsSuccessStatusCode)
            capi = ServerEvents(await stats.Content.ReadAsStringAsync(cancellationToken));
        return new MetaSignals(true, capi, null);
    }

    private async Task<HttpResponseMessage> Send(string path, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.facebook.com/v21.0/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await http.SendAsync(request, cancellationToken);
    }

    /// <summary>Kaynak kırılımında sunucu event'i varsa CAPI vardır. Biçim tanınmazsa boş döner.</summary>
    private static bool? ServerEvents(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return null;
            var sawSource = false;
            var sawServer = false;
            foreach (var item in data.EnumerateArray())
                Walk(item, ref sawSource, ref sawServer);
            if (!sawSource) return null;
            return sawServer;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void Walk(JsonElement element, ref bool sawSource, ref bool sawServer)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            string? value = null;
            var count = 0m;
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("value") && property.Value.ValueKind == JsonValueKind.String) value = property.Value.GetString();
                if (property.NameEquals("count") && property.Value.TryGetDecimal(out var parsed)) count = parsed;
                if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array) Walk(property.Value, ref sawSource, ref sawServer);
            }
            if (value is not null)
            {
                sawSource = true;
                if (count > 0 && value.Contains("server", StringComparison.OrdinalIgnoreCase)) sawServer = true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray()) Walk(item, ref sawSource, ref sawServer);
        }
    }
}
