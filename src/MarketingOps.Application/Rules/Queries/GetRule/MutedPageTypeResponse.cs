namespace MarketingOps.Application.Rules.Queries.GetRule;

/// <summary>Yanlış alarm işaretiyle kuralın susturulduğu sayfa tipi; kimin, ne zaman ve neden susturduğu.</summary>
public sealed record MutedPageTypeResponse(Guid FindingId, string? PageTypeName, string? Reason, string? Actor, DateTimeOffset? At);
