using System.Net;
using System.Text.Json;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;

namespace MarketingOps.Infrastructure.Google;

/// <summary>Google OAuth 2.0 (web sunucusu akışı). Refresh token her seferinde alınsın diye prompt=consent ve access_type=offline istenir.</summary>
public sealed class GoogleAuthClient(HttpClient http, GoogleOptions options) : IGoogleAuthClient
{
    private const string AuthorizeEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.ClientId) && !string.IsNullOrWhiteSpace(options.ClientSecret);

    public string BuildAuthorizationUrl(string state, string redirectUri)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId!,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = string.Join(' ', GoogleConnectors.Scopes),
            ["access_type"] = "offline",
            // Hesap seçici her seferinde gösterilir: yanlış hesapla bağlanıldıysa başka hesap seçilebilsin.
            ["prompt"] = "consent select_account",
            ["include_granted_scopes"] = "true",
            ["state"] = state
        };
        return AuthorizeEndpoint + "?" + string.Join("&", query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
    }

    public async Task<GoogleAuthorization> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        var token = await TokenAsync(new Dictionary<string, string>
        {
            ["code"] = code, ["client_id"] = options.ClientId!, ["client_secret"] = options.ClientSecret!,
            ["redirect_uri"] = redirectUri, ["grant_type"] = "authorization_code"
        }, cancellationToken);
        var refresh = token.String("refresh_token") ?? throw new GoogleApiException("Google refresh token vermedi. Bağlamayı yeniden deneyin.");
        var access = token.String("access_token")!;
        var scopes = (token.String("scope") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var missing = GoogleConnectors.Scopes.Where(s => s.StartsWith("https://", StringComparison.Ordinal) && !scopes.Contains(s)).ToList();
        if (missing.Count > 0)
            throw new GoogleApiException("İstenen salt okunur izinlerin hepsi verilmedi. Google ekranında tüm kutuları işaretleyerek yeniden bağlanın.");

        var user = await GoogleHttp.SendAsync(http, HttpMethod.Get, UserInfoEndpoint, access, null, cancellationToken);
        return new GoogleAuthorization(refresh, access, user.String("email") ?? "", scopes);
    }

    public async Task<string> GetAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var token = await TokenAsync(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken, ["client_id"] = options.ClientId!, ["client_secret"] = options.ClientSecret!, ["grant_type"] = "refresh_token"
        }, cancellationToken);
        return token.String("access_token") ?? throw new GoogleApiException("Google erişim token'ı vermedi.");
    }

    private async Task<JsonElement> TokenAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        if (!IsConfigured) throw new GoogleApiException("Google OAuth istemcisi ayarlı değil.");
        using var response = await http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var json = JsonDocument.Parse(body).RootElement.Clone();
        if (response.IsSuccessStatusCode) return json;
        // invalid_grant: kullanıcı erişimi kaldırdı ya da token süresi doldu.
        var error = json.String("error");
        throw new GoogleApiException(error == "invalid_grant" ? "Google yetkisi geri alınmış ya da süresi dolmuş." : $"Google token isteği reddedildi: {error}",
            isAuthorizationError: error == "invalid_grant" || response.StatusCode == HttpStatusCode.Unauthorized);
    }
}
