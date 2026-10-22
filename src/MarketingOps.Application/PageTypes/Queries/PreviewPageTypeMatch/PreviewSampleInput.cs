namespace MarketingOps.Application.PageTypes.Queries.PreviewPageTypeMatch;

/// <summary>Önizlenecek URL. Schema koşulları için sayfanın schema @type'ları verilebilir.</summary>
public sealed record PreviewSampleInput(string Url, IReadOnlyList<string>? SchemaTypes = null);
