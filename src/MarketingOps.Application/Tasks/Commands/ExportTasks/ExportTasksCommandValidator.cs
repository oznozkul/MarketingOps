using FluentValidation;

namespace MarketingOps.Application.Tasks.Commands.ExportTasks;

public sealed class ExportTasksCommandValidator : AbstractValidator<ExportTasksCommand>
{
    private const int MaxItems = 100;

    public ExportTasksCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x).Must(x => x.TaskIds.Count + (x.FindingIds?.Count ?? 0) > 0).OverridePropertyName("TaskIds").WithMessage("En az bir görev seçin.");
        RuleFor(x => x).Must(x => x.TaskIds.Count + (x.FindingIds?.Count ?? 0) <= MaxItems).OverridePropertyName("TaskIds")
            .WithMessage($"Tek seferde en fazla {MaxItems} görev aktarılabilir.");
    }
}
