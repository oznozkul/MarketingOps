using FluentValidation;

namespace MarketingOps.Application.Sites.Commands.UpdateExpectedTrackingIds;

/// <summary>Biçim kontrolü domain'dedir (ExpectedTrackingIds); burada yalnızca uzunluk sınırları var.</summary>
public sealed class UpdateExpectedTrackingIdsCommandValidator : AbstractValidator<UpdateExpectedTrackingIdsCommand>
{
    private const int IdMaxLength = 30;

    public UpdateExpectedTrackingIdsCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.Ga4MeasurementId).MaximumLength(IdMaxLength).WithMessage("GA4 Measurement ID çok uzun.");
        RuleFor(x => x.GtmContainerId).MaximumLength(IdMaxLength).WithMessage("GTM Container ID çok uzun.");
        RuleFor(x => x.MetaPixelId).MaximumLength(IdMaxLength).WithMessage("Meta Pixel ID çok uzun.");
        RuleFor(x => x.GoogleAdsConversionId).MaximumLength(IdMaxLength).WithMessage("Google Ads Conversion ID çok uzun.");
    }
}
