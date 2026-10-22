using FluentValidation;

namespace MarketingOps.Application.Connectors.Commands.ConnectMeta;

public sealed class ConnectMetaCommandValidator : AbstractValidator<ConnectMetaCommand>
{
    public ConnectMetaCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.AccessToken).MaximumLength(2000).WithMessage("Meta token'ı çok uzun.");
    }
}
