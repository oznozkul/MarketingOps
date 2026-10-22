using FluentValidation;
using MarketingOps.Domain.Rules;

namespace MarketingOps.Application.Rules.Commands.UpdateSiteRuleSetting;

public sealed class UpdateSiteRuleSettingCommandValidator : AbstractValidator<UpdateSiteRuleSettingCommand>
{
    public UpdateSiteRuleSettingCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.RuleCode).NotEmpty().WithMessage("Kural kodu zorunludur.")
            .Must(code => RuleCatalog.Find(code) is not null).WithMessage("Kural bulunamadı.");
        RuleFor(x => x.SeverityOverride).IsInEnum().WithMessage("Geçersiz önem.");
        RuleForEach(x => x.PageTypeSeverities.Values).IsInEnum().WithMessage("Geçersiz önem.");
        RuleFor(x => x).Custom((command, context) =>
        {
            if (RuleCatalog.Find(command.RuleCode) is { } rule) context.ValidateThresholds(rule, command.Thresholds);
        });
    }
}
