using FluentValidation;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Sites.Commands.UpdateTaskKeyPrefix;

public sealed class UpdateTaskKeyPrefixCommandValidator : AbstractValidator<UpdateTaskKeyPrefixCommand>
{
    public UpdateTaskKeyPrefixCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.Prefix).NotEmpty().WithMessage("Görev öneki zorunludur.")
            .Matches("^[A-Za-z][A-Za-z0-9]*$").WithMessage("Görev öneki harfle başlamalı ve yalnızca harf ve rakam içermeli.")
            .MaximumLength(Site.TaskKeyPrefixMaxLength).WithMessage($"Görev öneki en fazla {Site.TaskKeyPrefixMaxLength} karakter olabilir.");
    }
}
