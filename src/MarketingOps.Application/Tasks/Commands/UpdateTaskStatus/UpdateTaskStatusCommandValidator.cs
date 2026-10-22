using FluentValidation;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.UpdateTaskStatus;

public sealed class UpdateTaskStatusCommandValidator : AbstractValidator<UpdateTaskStatusCommand>
{
    public UpdateTaskStatusCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Görev kimliği zorunludur.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Geçersiz durum.");
    }
}
