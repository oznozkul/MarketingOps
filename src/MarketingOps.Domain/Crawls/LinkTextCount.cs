namespace MarketingOps.Domain.Crawls;

/// <summary>Sayfadaki kısa bir link metni (küçük harf) ve kaç linkte geçtiği.</summary>
public sealed record LinkTextCount(string Text, int Count);
