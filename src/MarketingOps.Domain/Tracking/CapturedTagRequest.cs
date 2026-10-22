namespace MarketingOps.Domain.Tracking;

/// <summary>Denetimde yakalanan bir tag isteği. Onay tıklanmadan önce gelenler ayrı işaretlenir.</summary>
public sealed record CapturedTagRequest(string Url, TagVendor Vendor, string? Id, string? Gcs, string? Gcd, bool BeforeConsent);
