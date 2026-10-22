using FluentValidation;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.AssignTaskTeam;

public sealed class AssignTaskTeamCommandValidator : AbstractValidator<AssignTaskTeamCommand>
{
    public AssignTaskTeamCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty().WithMessage("Site kimliği zorunludur.");
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Görev kimliği zorunludur.");
        RuleFor(x => x.Team).IsInEnum().WithMessage("Geçersiz ekip.");
    }
}
