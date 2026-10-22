using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Notifications;

public static class NotificationAddress
{
    public const int MaxCount = 20;
    public const int MaxLength = 200;

    public static List<string> Normalize(IEnumerable<string>? raw)
    {
        var list = (raw ?? [])
            .SelectMany(value => value.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(value => value.Trim().ToLowerInvariant())
            .Where(value => value.Length > 0)
            .Distinct()
            .ToList();
        if (list.Count > MaxCount) throw new DomainException($"En fazla {MaxCount} alıcı yazılabilir.");
        foreach (var email in list)
        {
            if (email.Length > MaxLength || !IsEmail(email))
                throw new DomainException($"'{email}' geçerli bir e-posta değil.");
        }
        return list;
    }

    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@');
        return at > 0 && at < value.Length - 1 && !value.Contains(' ') && value.IndexOf('@', at + 1) < 0;
    }
}
