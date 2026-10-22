using System.Text.RegularExpressions;

namespace MarketingOps.Domain.Sites;

/// <summary>
/// Sipariş karşılaştırmasının SQL Server kaynağı. Bağlantı cümlesi altyapıda şifrelenir; domain yalnızca şifreli değeri taşır.
/// Sorgu tek bir parametreli SELECT olmalıdır.
/// </summary>
public sealed partial record OrderSourceSettings
{
    public const int QueryMaxLength = 4000;

    public string? ProtectedConnection { get; }
    public string? Query { get; }

    public bool IsConfigured => ProtectedConnection is not null && Query is not null;

    public static OrderSourceSettings None => new(null, null);

    public OrderSourceSettings(string? protectedConnection, string? query)
    {
        ProtectedConnection = string.IsNullOrWhiteSpace(protectedConnection) ? null : protectedConnection;
        Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim().TrimEnd(';').Trim();
        if (ProtectedConnection is null && Query is null) return;
        if (ProtectedConnection is null || Query is null)
            throw new Common.DomainException("Sipariş kaynağı için bağlantı cümlesi ve sorgu birlikte verilmeli.");
        if (Query.Length > QueryMaxLength)
            throw new Common.DomainException($"Sipariş sorgusu en fazla {QueryMaxLength} karakter olabilir.");
        if (!SelectOnly().IsMatch(Query) || Forbidden().IsMatch(Query))
            throw new Common.DomainException("Sipariş sorgusu tek bir SELECT olmalı; başka komut ya da birden fazla ifade yazılamaz.");
        if (!Query.Contains("@date", StringComparison.OrdinalIgnoreCase))
            throw new Common.DomainException("Sipariş sorgusu günü @date parametresiyle almalı.");
    }

    private OrderSourceSettings() { }

    [GeneratedRegex(@"^\s*select\b[\s\S]*$", RegexOptions.IgnoreCase)]
    private static partial Regex SelectOnly();

    [GeneratedRegex(@";|--|/\*|\b(insert|update|delete|drop|alter|exec|execute|merge|truncate|grant|revoke)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Forbidden();
}
