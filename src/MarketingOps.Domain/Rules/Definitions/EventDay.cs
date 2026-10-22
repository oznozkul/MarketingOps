namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>Bir event'in dünkü sayısı ve önceki 7 günün ortalaması.</summary>
public sealed record EventDay(string Name, long Yesterday, decimal Average);
