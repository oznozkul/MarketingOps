namespace MarketingOps.Domain.Tracking;

/// <summary>Denetimde açılan bir temsilci sayfa ve yakalanan tag istekleri.</summary>
public sealed record AuditedPage(string Label, Guid? PageTypeId, string Url, IReadOnlyList<CapturedTagRequest> Requests);
