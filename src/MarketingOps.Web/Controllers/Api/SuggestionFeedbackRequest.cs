namespace MarketingOps.Web.Controllers.Api;

public sealed record SuggestionFeedbackRequest(bool IsHelpful, string? Note);
