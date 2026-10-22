using FluentValidation;

namespace MarketingOps.Application.Notifications.Commands.SaveNotificationRule;

public sealed class SaveNotificationRuleCommandValidator : AbstractValidator<SaveNotificationRuleCommand>
{
    public SaveNotificationRuleCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.RepeatIntervalHours).InclusiveBetween(NotificationRuleMin(), NotificationRuleMax()).WithMessage("Tekrar aralığı 1 ile 168 saat arasında olmalı.");
    }

    private static int NotificationRuleMin() => Domain.Notifications.NotificationRule.MinRepeatHours;
    private static int NotificationRuleMax() => Domain.Notifications.NotificationRule.MaxRepeatHours;
}
