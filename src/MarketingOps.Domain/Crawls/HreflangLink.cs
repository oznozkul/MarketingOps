namespace MarketingOps.Domain.Crawls;

/// <summary>link[rel=alternate][hreflang] öğesi: dil/bölge kodu ve karşılık gelen adres.</summary>
public sealed record HreflangLink(string Code, string Url);
