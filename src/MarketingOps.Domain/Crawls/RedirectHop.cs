namespace MarketingOps.Domain.Crawls;

/// <summary>Yönlendirme zincirindeki bir adım: istenen URL ve dönen durum kodu.</summary>
public sealed record RedirectHop(string Url, int StatusCode);
