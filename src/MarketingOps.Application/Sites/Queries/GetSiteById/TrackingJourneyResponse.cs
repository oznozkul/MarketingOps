namespace MarketingOps.Application.Sites.Queries.GetSiteById;

public sealed record TrackingJourneyResponse(string? CartUrl, string? CheckoutUrl, string? ThankYouUrl, string? ConsentButtonSelector);
