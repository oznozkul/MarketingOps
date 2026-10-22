namespace MarketingOps.Application.Rules.Queries.GetRule;

/// <summary><see cref="Value"/> site ayarındaki değer; ayar yoksa varsayılandır.</summary>
public sealed record RuleThresholdResponse(string Key, string Label, string Unit, decimal Default, decimal Min, decimal Max, decimal Value);
