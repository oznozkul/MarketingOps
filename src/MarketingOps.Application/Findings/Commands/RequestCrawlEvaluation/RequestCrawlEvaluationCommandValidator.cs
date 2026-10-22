using FluentValidation;

namespace MarketingOps.Application.Findings.Commands.RequestCrawlEvaluation;

public sealed class RequestCrawlEvaluationCommandValidator : AbstractValidator<RequestCrawlEvaluationCommand>
{
    public RequestCrawlEvaluationCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.CrawlId).NotEmpty().WithMessage("Tarama kimliği zorunludur.");
    }
}
