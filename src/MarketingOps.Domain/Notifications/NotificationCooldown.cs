namespace MarketingOps.Domain.Notifications;

public static class NotificationCooldown
{
    /// <summary>Aynı bulgu ve olay, aralık dolmadan yeniden gitmez. Aralık sıfırsa her seferinde gider.</summary>
    public static bool Allows(DateTimeOffset? lastSent, DateTimeOffset now, int intervalHours) =>
        intervalHours <= 0 || lastSent is null || now - lastSent.Value >= TimeSpan.FromHours(intervalHours);
}
