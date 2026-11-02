namespace MarketingOps.Application.PageTypes.Suggestions;

/// <summary>Aynı URL kalıbını paylaşan sayfalar. <see cref="Pattern"/> doğrudan sayfa tipi glob'u olarak kullanılabilir.</summary>
public sealed record UrlGroup(string Pattern, int Count, IReadOnlyList<string> SampleUrls);
