namespace MarketingOps.Application.Connectors.Queries.GetGoogleProperties;

public sealed record GooglePropertiesResponse(
    string? AccountEmail,
    IReadOnlyList<SearchConsolePropertyResponse> SearchConsoleSites,
    IReadOnlyList<Ga4PropertyResponse> Ga4Properties,
    string? SelectedSearchConsoleSite,
    string? SelectedGa4Property);
