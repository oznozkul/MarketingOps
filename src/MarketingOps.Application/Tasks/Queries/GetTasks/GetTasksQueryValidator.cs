using FluentValidation;
using MarketingOps.Application.Common.Validation;

namespace MarketingOps.Application.Tasks.Queries.GetTasks;

public sealed class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
{
    public GetTasksQueryValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.Search).MaximumLength(200).WithMessage("Arama metni en fazla 200 karakter olabilir.");
        this.AddPagingRules(x => x.Page, x => x.PageSize);
    }
}
