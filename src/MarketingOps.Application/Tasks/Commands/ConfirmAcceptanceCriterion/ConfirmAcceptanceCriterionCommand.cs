using MediatR;

namespace MarketingOps.Application.Tasks.Commands.ConfirmAcceptanceCriterion;

/// <summary>Yalnızca ekip onayı tipindeki kriterler; otomatik olanlar taramayla işaretlenir. Confirmed=false onayı geri alır.</summary>
public sealed record ConfirmAcceptanceCriterionCommand(Guid SiteId, Guid TaskId, Guid CriterionId, bool Confirmed, string Actor) : IRequest;
