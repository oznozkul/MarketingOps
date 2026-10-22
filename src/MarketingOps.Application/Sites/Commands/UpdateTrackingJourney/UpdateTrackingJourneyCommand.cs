using MediatR;

namespace MarketingOps.Application.Sites.Commands.UpdateTrackingJourney;

public sealed record UpdateTrackingJourneyCommand(Guid SiteId, string? CartUrl, string? CheckoutUrl, string? ThankYouUrl, string? ConsentButtonSelector) : IRequest;
