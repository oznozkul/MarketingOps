using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Tasks.Tracking;

namespace MarketingOps.Infrastructure.Tracking;

/// <summary>
/// Jira Cloud REST API v3. Açıklama Atlassian Document Format'a (paragraflar) çevrilir. Jira'dan durum geri okunmaz.
/// </summary>
public sealed class JiraTaskTracker(HttpClient http) : ITaskTracker
{
    public async Task<ExternalIssue> UpsertAsync(TaskTrackerCredentials credentials, ExternalIssueRequest request, CancellationToken cancellationToken)
    {
        var fields = new JsonObject
        {
            ["summary"] = request.Title.Length > 250 ? request.Title[..250] : request.Title,
            ["description"] = Document(request.Description),
            ["labels"] = new JsonArray(request.Labels.Select(l => (JsonNode)l).ToArray())
        };

        if (request.ExistingKey is { } key)
        {
            using var update = Request(HttpMethod.Put, credentials, $"/rest/api/3/issue/{Uri.EscapeDataString(key)}", new JsonObject { ["fields"] = fields });
            await SendAsync(update, cancellationToken);
            return new ExternalIssue(key, Browse(credentials, key));
        }

        fields["project"] = new JsonObject { ["key"] = credentials.ProjectKey };
        fields["issuetype"] = new JsonObject { ["name"] = credentials.IssueType };
        using var create = Request(HttpMethod.Post, credentials, "/rest/api/3/issue", new JsonObject { ["fields"] = fields });
        using var response = await SendAsync(create, cancellationToken);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var newKey = created.GetProperty("key").GetString() ?? throw new TaskTrackerException("Jira kayıt anahtarı dönmedi.");
        return new ExternalIssue(newKey, Browse(credentials, newKey));
    }

    private static HttpRequestMessage Request(HttpMethod method, TaskTrackerCredentials credentials, string path, JsonObject body)
    {
        var message = new HttpRequestMessage(method, credentials.BaseUrl.TrimEnd('/') + path) { Content = JsonContent.Create(body) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credentials.Email}:{credentials.ApiToken}")));
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return message;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new TaskTrackerException("Jira'ya ulaşılamadı. Adresi kontrol edin.", ex);
        }
        if (response.IsSuccessStatusCode) return response;

        var body = await response.Content.ReadAsStringAsync(ct);
        response.Dispose();
        throw new TaskTrackerException(response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Jira kimlik bilgileri reddedildi. E-posta ve API anahtarını kontrol edin.",
            HttpStatusCode.NotFound => "Jira kaydı ya da proje bulunamadı.",
            _ => $"Jira isteği reddetti ({(int)response.StatusCode}): {Truncate(body)}"
        });
    }

    /// <summary>Düz metni ADF paragraflarına çevirir; boş satırlar paragraf ayırır.</summary>
    private static JsonObject Document(string text) => new()
    {
        ["type"] = "doc",
        ["version"] = 1,
        ["content"] = new JsonArray(text.Replace("\r", "").Split('\n')
            .Where(line => line.Length > 0)
            .Select(line => (JsonNode)new JsonObject
            {
                ["type"] = "paragraph",
                ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = line })
            }).ToArray())
    };

    private static string Browse(TaskTrackerCredentials credentials, string key) => $"{credentials.BaseUrl.TrimEnd('/')}/browse/{key}";

    private static string Truncate(string value) => value.Length > 300 ? value[..300] : value;
}
