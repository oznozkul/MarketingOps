using MediatR;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.SubmitSuggestionFeedback;

public sealed record SubmitSuggestionFeedbackCommand(Guid SiteId, Guid TaskId, bool IsHelpful, string? Note, string Author) : IRequest;
