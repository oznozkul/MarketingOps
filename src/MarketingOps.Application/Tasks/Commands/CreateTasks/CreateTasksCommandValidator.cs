using FluentValidation;

namespace MarketingOps.Application.Tasks.Commands.CreateTasks;

public sealed class CreateTasksCommandValidator : AbstractValidator<CreateTasksCommand>
{
    private const int MaxItems = 500;

    public CreateTasksCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.FindingIds).Must(ids => ids is null || ids.Count <= MaxItems).WithMessage($"Tek seferde en fazla {MaxItems} bulgu seçilebilir.");
    }
}
