using FluentValidation;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Sites.Commands.UpdateCrawlSettings;

public sealed class UpdateCrawlSettingsCommandValidator : AbstractValidator<UpdateCrawlSettingsCommand>
{
    private const int MaxExcludedPatterns = 50;
    private const int PatternMaxLength = 300;

    public UpdateCrawlSettingsCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.Schedule).IsInEnum().WithMessage("Geçersiz tarama sıklığı.");
        RuleFor(x => x.MaxRequestsPerSecond)
            .InclusiveBetween(CrawlSettings.MinRequestsPerSecond, CrawlSettings.MaxRequestsPerSecondLimit)
            .WithMessage($"Hız limiti {CrawlSettings.MinRequestsPerSecond} ile {CrawlSettings.MaxRequestsPerSecondLimit} istek/saniye arasında olmalı.");
        RuleFor(x => x.MaxPages).InclusiveBetween(1, CrawlSettings.MaxPagesLimit)
            .WithMessage($"Maksimum sayfa sayısı 1 ile {CrawlSettings.MaxPagesLimit} arasında olmalı.");
        RuleFor(x => x.ExcludedPathPatterns).NotNull().WithMessage("Hariç tutulan path listesi boş bırakılamaz.")
            .Must(p => p.Count <= MaxExcludedPatterns).WithMessage($"En fazla {MaxExcludedPatterns} hariç tutulan path olabilir.");
        RuleForEach(x => x.ExcludedPathPatterns).NotEmpty().WithMessage("Hariç tutulan path boş olamaz.")
            .MaximumLength(PatternMaxLength).WithMessage($"Hariç tutulan path en fazla {PatternMaxLength} karakter olabilir.");
    }
}
