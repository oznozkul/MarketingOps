namespace MarketingOps.Application.Findings.Commands;

/// <summary>Erteleme tarihi kullanıcının gününe göre seçilir; o günün başlangıcı (Türkiye saati) kullanılır.</summary>
public static class SnoozeDate
{
    private static readonly TimeSpan Turkey = TimeSpan.FromHours(3);

    public static DateTimeOffset ToInstant(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), Turkey);
}
