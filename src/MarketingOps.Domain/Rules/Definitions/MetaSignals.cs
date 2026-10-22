namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>
/// Meta Graph'tan okunan kanıt. CAPI veya tekilleştirme kanıtlanamazsa ilgili alan boş kalır ve kural bulgu açmaz.
/// </summary>
public sealed record MetaSignals(bool PixelActive, bool? CapiActive, bool? EventIdDedup);
