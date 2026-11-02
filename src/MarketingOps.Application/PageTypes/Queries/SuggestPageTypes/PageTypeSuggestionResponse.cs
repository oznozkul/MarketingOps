namespace MarketingOps.Application.PageTypes.Queries.SuggestPageTypes;

public sealed record PageTypeSuggestionResponse(string Name, string UrlGlob, string Reason, int MatchCount, IReadOnlyList<string> SampleUrls);
