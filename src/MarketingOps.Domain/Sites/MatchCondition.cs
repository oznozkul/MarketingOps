using System.Text.RegularExpressions;
using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Sites;

/// <summary>Sayfa tipini tanımlayan tek bir koşul. Desen oluşturulurken doğrulanır.</summary>
public sealed record MatchCondition
{
    public const int ValueMaxLength = 500;

    public MatchConditionType Type { get; }
    public string Value { get; }

    public MatchCondition(MatchConditionType type, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Koşul değeri boş olamaz.");

        Type = type;
        Value = value.Trim();

        if (Type == MatchConditionType.UrlRegex)
            EnsureValidRegex(Value);
    }

    public bool IsMatch(Uri url, IReadOnlyCollection<string> schemaTypes) => Type switch
    {
        MatchConditionType.UrlGlob => GlobPattern.ToRegex(Value).IsMatch(url.AbsolutePath),
        MatchConditionType.UrlRegex => new Regex(Value, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)).IsMatch(url.PathAndQuery),
        _ => schemaTypes.Contains(Value, StringComparer.OrdinalIgnoreCase)
    };

    private static void EnsureValidRegex(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }
        catch (ArgumentException)
        {
            throw new DomainException($"Geçersiz düzenli ifade: {pattern}");
        }
    }
}
