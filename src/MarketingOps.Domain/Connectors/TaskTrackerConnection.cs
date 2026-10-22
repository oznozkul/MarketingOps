using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Connectors;

/// <summary>
/// Sitenin dış görev sistemi (Jira Cloud) bağlantısı. API anahtarı şifrelenmiş olarak tutulur; şifreleme altyapıdadır,
/// domain yalnızca şifreli değeri taşır.
/// </summary>
public class TaskTrackerConnection : Entity
{
    public Guid SiteId { get; private set; }
    public string BaseUrl { get; private set; } = null!;
    public string ProjectKey { get; private set; } = null!;
    public string IssueType { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string ProtectedApiToken { get; private set; } = null!;
    public DateTimeOffset UpdatedAt { get; private set; }

    private TaskTrackerConnection() { }

    public static TaskTrackerConnection Create(Guid siteId, string baseUrl, string projectKey, string issueType, string email, string protectedApiToken, DateTimeOffset now)
    {
        var connection = new TaskTrackerConnection { SiteId = siteId };
        connection.Update(baseUrl, projectKey, issueType, email, protectedApiToken, now);
        return connection;
    }

    /// <summary><paramref name="protectedApiToken"/> null ise kayıtlı anahtar korunur (formda anahtar tekrar istenmez).</summary>
    public void Update(string baseUrl, string projectKey, string issueType, string email, string? protectedApiToken, DateTimeOffset now)
    {
        if (!Uri.TryCreate(baseUrl?.Trim(), UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
            throw new DomainException("Jira adresi https:// ile başlayan geçerli bir adres olmalı.");
        if (protectedApiToken is null && ProtectedApiToken is null) throw new DomainException("API anahtarı zorunludur.");

        BaseUrl = url.GetLeftPart(UriPartial.Authority);
        ProjectKey = projectKey.Trim().ToUpperInvariant();
        IssueType = issueType.Trim();
        Email = email.Trim();
        if (protectedApiToken is not null) ProtectedApiToken = protectedApiToken;
        UpdatedAt = now;
    }
}
