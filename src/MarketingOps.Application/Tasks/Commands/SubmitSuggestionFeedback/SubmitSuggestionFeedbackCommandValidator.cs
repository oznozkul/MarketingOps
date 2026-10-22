using FluentValidation;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.SubmitSuggestionFeedback;

public sealed class SubmitSuggestionFeedbackCommandValidator : AbstractValidator<SubmitSuggestionFeedbackCommand>
{
    public SubmitSuggestionFeedbackCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Görev kimliği zorunludur.");
        RuleFor(x => x.Note).MaximumLength(WorkTask.FeedbackNoteMaxLength).WithMessage($"Not en fazla {WorkTask.FeedbackNoteMaxLength} karakter olabilir.");
    }
}
