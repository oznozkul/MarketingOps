using System.Globalization;

namespace MarketingOps.Web.Infrastructure;

/// <summary>tr-TR biçimlendirme yardımcıları: 1.206, %38, 21 Eki.</summary>
public static class Format
{
    public static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Tarih ve saatler sitenin saat diliminde (Türkiye, UTC+3) gösterilir.</summary>
    public static readonly TimeSpan DisplayOffset = TimeSpan.FromHours(3);

    private const char Minus = '−';

    public static string Number(long value) => value.ToString("N0", Tr);

    public static string Number(double value, int decimals = 1) => value.ToString("N" + decimals, Tr);

    /// <summary>Türkçe yüzde: işaret önde (%38).</summary>
    public static string Percent(double value, int decimals = 0)
    {
        var abs = Math.Abs(value).ToString(decimals == 0 ? "0" : "0." + new string('#', decimals), Tr);
        return (value < 0 ? Minus.ToString() : "") + "%" + abs;
    }

    public static string SignedPercent(double value, int decimals = 1) =>
        (value > 0 ? "+" : "") + Percent(value, decimals);

    public static string Signed(int value) => value switch
    {
        > 0 => "+" + value.ToString(Tr),
        < 0 => Minus + Math.Abs(value).ToString(Tr),
        _ => "0"
    };

    private static DateTimeOffset Local(DateTimeOffset value) => value.ToOffset(DisplayOffset);

    public static string ShortDate(DateTimeOffset value) => Local(value).ToString("d MMM", Tr);

    public static string ShortDate(DateOnly value) => value.ToString("d MMM", Tr);

    public static string DateTime(DateTimeOffset value) => Local(value).ToString("d MMM HH:mm", Tr);

    public static string Duration(TimeSpan? value)
    {
        if (value is null) return "—";
        var v = value.Value;
        if (v.TotalMinutes < 1) return $"{Math.Max(0, (int)v.TotalSeconds)} sn";
        if (v.TotalHours < 1) return $"{(int)v.TotalMinutes} dk {v.Seconds} sn";
        return v.Minutes == 0 ? $"{(int)v.TotalHours} sa" : $"{(int)v.TotalHours} sa {v.Minutes} dk";
    }

    public static string Iso(DateTimeOffset value) => value.ToString("o", CultureInfo.InvariantCulture);

    public static string Initials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpper(p[0], Tr)));
}
