namespace MarketingOps.Application.PageTypes.Suggestions;

public sealed record PageTypeSuggestionRequest(string Domain, IReadOnlyList<UrlGroup> Groups, IReadOnlyList<string> ExistingPageTypes);
