namespace MarketingOps.Application.Connectors.Commands.CompleteGoogleConnect;

public sealed record GoogleConnectResult(Guid SiteId, bool Connected, string? Message);
