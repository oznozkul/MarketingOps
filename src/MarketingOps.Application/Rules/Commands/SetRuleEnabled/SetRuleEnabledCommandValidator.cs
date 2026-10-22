using FluentValidation;
using MarketingOps.Domain.Rules;

namespace MarketingOps.Application.Rules.Commands.SetRuleEnabled;

public sealed class SetRuleEnabledCommandValidator : AbstractValidator<SetRuleEnabledCommand>
{
    public SetRuleEnabledCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.RuleCode).Must(code => RuleCatalog.Find(code) is not null).WithMessage("Kural bulunamadı.");
    }
}
