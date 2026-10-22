using MarketingOps.Application.Sites.Commands.UpdateTrackingJourney;
using MarketingOps.Application.Sites.Queries.GetSiteById;

namespace MarketingOps.Web.ViewModels.Sites;

public sealed class TrackingJourneyForm
{
    public string? CartUrl { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? ThankYouUrl { get; set; }
    public string? ConsentButtonSelector { get; set; }

    public static TrackingJourneyForm From(TrackingJourneyResponse journey) => new()
    {
        CartUrl = journey.CartUrl,
        CheckoutUrl = journey.CheckoutUrl,
        ThankYouUrl = journey.ThankYouUrl,
        ConsentButtonSelector = journey.ConsentButtonSelector
    };

    public UpdateTrackingJourneyCommand ToCommand(Guid siteId) =>
        new(siteId, CartUrl, CheckoutUrl, ThankYouUrl, ConsentButtonSelector);
}
