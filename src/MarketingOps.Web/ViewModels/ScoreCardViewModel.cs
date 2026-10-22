using MarketingOps.Web.TagHelpers;

namespace MarketingOps.Web.ViewModels;

/// <summary>
/// Skor kartı. <see cref="Value"/> null ise gerekli veri kaynağı bağlı değildir ve kart "Veri yok" durumunu gösterir.
/// </summary>
public sealed record ScoreCardViewModel(
    string Title,
    int? Value,
    int? Previous,
    string? BreakdownUrl,
    string? MissingConnector = null,
    string? ConnectUrl = null)
{
    public int? Delta => Value is null || Previous is null ? null : Value - Previous;

    public (string Label, Tone Tone) Band => Value switch
    {
        >= 90 => ("İyi", Tone.Ok),
        >= 70 => ("Orta", Tone.Warn),
        _ => ("Zayıf", Tone.Critical)
    };
}
