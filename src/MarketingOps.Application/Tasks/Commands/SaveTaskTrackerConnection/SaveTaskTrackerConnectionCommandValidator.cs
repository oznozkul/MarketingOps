using FluentValidation;

namespace MarketingOps.Application.Tasks.Commands.SaveTaskTrackerConnection;

public sealed class SaveTaskTrackerConnectionCommandValidator : AbstractValidator<SaveTaskTrackerConnectionCommand>
{
    public SaveTaskTrackerConnectionCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.BaseUrl).NotEmpty().WithMessage("Jira adresi zorunludur.")
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps)
            .WithMessage("Jira adresi https:// ile başlamalı (ör. https://sirket.atlassian.net).");
        RuleFor(x => x.ProjectKey).NotEmpty().WithMessage("Proje anahtarı zorunludur.")
            .Matches("^[A-Za-z][A-Za-z0-9_]{1,19}$").WithMessage("Proje anahtarı harfle başlamalı (ör. SEO).");
        RuleFor(x => x.IssueType).NotEmpty().WithMessage("Kayıt türü zorunludur.").MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-posta zorunludur.").EmailAddress().WithMessage("Geçerli bir e-posta girin.");
        RuleFor(x => x.ApiToken).MaximumLength(500).WithMessage("API anahtarı çok uzun.");
    }
}
