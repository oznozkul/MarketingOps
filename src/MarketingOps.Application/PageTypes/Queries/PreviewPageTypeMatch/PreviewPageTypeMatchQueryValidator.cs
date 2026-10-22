using FluentValidation;

namespace MarketingOps.Application.PageTypes.Queries.PreviewPageTypeMatch;

public sealed class PreviewPageTypeMatchQueryValidator : AbstractValidator<PreviewPageTypeMatchQuery>
{
    private const int MaxSamples = 500;

    public PreviewPageTypeMatchQueryValidator()
    {
        this.AddConditionRules(x => x.Mode, x => x.Conditions);
        RuleFor(x => x.Samples).NotEmpty().WithMessage("En az bir örnek URL gerekli.")
            .Must(s => s is null || s.Count <= MaxSamples).WithMessage($"En fazla {MaxSamples} örnek URL gönderilebilir.");
        RuleForEach(x => x.Samples).ChildRules(sample =>
            sample.RuleFor(s => s.Url)
                .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .WithMessage("Örnek URL http(s) ile başlayan tam bir adres olmalı."));
    }
}
