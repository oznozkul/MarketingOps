namespace MarketingOps.Application.Connectors.Google;

public sealed record GoogleAuthorization(string RefreshToken, string AccessToken, string Email, IReadOnlyList<string> Scopes);
