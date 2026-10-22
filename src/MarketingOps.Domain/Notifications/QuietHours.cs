using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Notifications;

/// <summary>İstanbul saatine göre sessiz aralık. Başlangıç bitişten büyükse aralık gece yarısını aşar. İkisi de boşsa kapalıdır.</summary>
public sealed record QuietHours(TimeOnly? Start, TimeOnly? End)
{
    public static QuietHours None { get; } = new(null, null);

    public static QuietHours Create(TimeOnly? start, TimeOnly? end)
    {
        if (start is null != end is null) throw new DomainException("Sessiz saat için başlangıç ve bitiş birlikte yazılır.");
        if (start == end && start is not null) throw new DomainException("Sessiz saat aralığı boş olamaz.");
        return new QuietHours(start, end);
    }

    /// <summary>Aralık dışındaysa null. İçindeyse aralığın bittiği UTC anı.</summary>
    public DateTimeOffset? DelayUntil(DateTimeOffset utcNow)
    {
        if (Start is null || End is null) return null;
        var local = IstanbulClock.Local(utcNow);
        var time = TimeOnly.FromDateTime(local.DateTime);
        var overnight = Start > End;
        var inside = overnight ? time >= Start || time < End : time >= Start && time < End;
        if (!inside) return null;

        var endDate = DateOnly.FromDateTime(local.DateTime);
        if (overnight && time >= Start) endDate = endDate.AddDays(1);
        var endLocal = endDate.ToDateTime(End.Value);
        var offset = IstanbulClock.Zone.GetUtcOffset(endLocal);
        return new DateTimeOffset(endLocal, offset);
    }
}
