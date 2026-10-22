using MarketingOps.Domain.Connectors;

namespace MarketingOps.Application.Connectors.Queries.GetConnectors;

public sealed record ConnectorResponse(
    ConnectorType Type,
    ConnectorStatus Status,
    string? AccountEmail,
    string? ExternalAccountId,
    string? ExternalAccountName,
    IReadOnlyList<string> Scopes,
    DateTimeOffset? LastSyncAt,
    string? LastError);
