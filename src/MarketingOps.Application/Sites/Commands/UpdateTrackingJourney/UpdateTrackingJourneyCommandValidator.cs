using FluentValidation;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Sites.Commands.UpdateTrackingJourney;

public sealed class UpdateTrackingJourneyCommandValidator : AbstractValidator<UpdateTrackingJourneyCommand>
{
    public UpdateTrackingJourneyCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.CartUrl).MaximumLength(2048).WithMessage("Sepet adresi çok uzun.");
        RuleFor(x => x.CheckoutUrl).MaximumLength(2048).WithMessage("Ödeme adresi çok uzun.");
        RuleFor(x => x.ThankYouUrl).MaximumLength(2048).WithMessage("Teşekkür adresi çok uzun.");
        RuleFor(x => x.ConsentButtonSelector).MaximumLength(TrackingJourney.SelectorMaxLength).WithMessage("Çerez onay seçicisi çok uzun.");
    }
}
