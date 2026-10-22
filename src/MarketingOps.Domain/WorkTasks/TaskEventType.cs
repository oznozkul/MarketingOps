namespace MarketingOps.Domain.WorkTasks;

public enum TaskEventType
{
    Created,
    StatusChanged,
    TeamAssigned,
    CriterionMet,
    CriterionReverted,
    Closed,
    Reopened,
    SuggestionGenerated
}
