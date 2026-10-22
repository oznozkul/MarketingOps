namespace MarketingOps.Domain.Common;

/// <summary>Ürün saatleri İstanbul'a göredir. Site başına saat dilimi yoktur.</summary>
public static class IstanbulClock
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static DateTimeOffset Local(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Zone);

    public static DateOnly Today(DateTimeOffset utc) => DateOnly.FromDateTime(Local(utc).DateTime);
}
