using FluentValidation;

namespace MarketingOps.Application.Connectors.Commands.SelectConnectorProperty;

public sealed class SelectConnectorPropertyCommandValidator : AbstractValidator<SelectConnectorPropertyCommand>
{
    public SelectConnectorPropertyCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x).Must(x => x.SearchConsoleSiteUrl is not null || x.Ga4PropertyId is not null)
            .OverridePropertyName("SearchConsoleSiteUrl").WithMessage("Değiştirilecek bir mülk seçin.");
    }
}
