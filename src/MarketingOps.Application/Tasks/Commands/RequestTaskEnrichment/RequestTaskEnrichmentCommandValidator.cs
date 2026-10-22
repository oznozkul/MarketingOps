using FluentValidation;

namespace MarketingOps.Application.Tasks.Commands.RequestTaskEnrichment;

public sealed class RequestTaskEnrichmentCommandValidator : AbstractValidator<RequestTaskEnrichmentCommand>
{
    public RequestTaskEnrichmentCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Görev kimliği zorunludur.");
    }
}
