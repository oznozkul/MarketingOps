using FluentValidation;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.AddTaskComment;

public sealed class AddTaskCommentCommandValidator : AbstractValidator<AddTaskCommentCommand>
{
    public AddTaskCommentCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Görev kimliği zorunludur.");
        RuleFor(x => x.Text).NotEmpty().WithMessage("Yorum boş olamaz.")
            .MaximumLength(WorkTask.CommentMaxLength).WithMessage($"Yorum en fazla {WorkTask.CommentMaxLength} karakter olabilir.");
    }
}
