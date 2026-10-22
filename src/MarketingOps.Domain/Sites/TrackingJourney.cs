namespace MarketingOps.Domain.Sites;

/// <summary>Tracking denetiminin açtığı sabit adresler. Hepsi opsiyoneldir; ödeme adımı simüle edilmez.</summary>
public sealed record TrackingJourney
{
    public const int SelectorMaxLength = 200;

    public string? CartUrl { get; }
    public string? CheckoutUrl { get; }
    public string? ThankYouUrl { get; }
    public string? ConsentButtonSelector { get; }

    public static TrackingJourney Empty => new(null, null, null, null);

    public TrackingJourney(string? cartUrl, string? checkoutUrl, string? thankYouUrl, string? consentButtonSelector)
    {
        CartUrl = Clean(cartUrl);
        CheckoutUrl = Clean(checkoutUrl);
        ThankYouUrl = Clean(thankYouUrl);
        ConsentButtonSelector = Clean(consentButtonSelector);
        if (ConsentButtonSelector?.Length > SelectorMaxLength)
            throw new Common.DomainException($"Çerez onay seçicisi en fazla {SelectorMaxLength} karakter olabilir.");
    }

    private TrackingJourney() { }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
