namespace MarketingOps.Application.Sites.Queries.GetSiteById;

/// <summary>Bağlantı cümlesi dönmez. Sorgu, formu yeniden doldurmak için açıktır.</summary>
public sealed record OrderSourceResponse(bool IsConfigured, string? Query);
