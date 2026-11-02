using System.Text.RegularExpressions;

namespace MarketingOps.Application.PageTypes.Suggestions;

/// <summary>
/// URL'leri path yapısına göre gruplar: alt klasördeki sayfalar ilk klasöre ("/urun/**"), kök düzeyindeki sayfalar
/// uzantılarına ("/*.html") ya da slug'daki tip işaretine ("/*-p-*") göre toplanır. E-ticaret sitelerinde ürün sayfaları
/// çoğu zaman kökte tek segmentlik slug olduğu için ilk segmente göre gruplamak her ürünü ayrı grup yapardı.
/// </summary>
public static partial class UrlGrouping
{
    private const int SamplesPerGroup = 5;
    private const int MaxGroups = 20;

    public static IReadOnlyList<UrlGroup> Group(IEnumerable<string> urls) =>
        urls.Select(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) ? uri : null)
            .OfType<Uri>()
            .DistinctBy(u => u.AbsolutePath)
            .GroupBy(u => PatternOf(u.AbsolutePath))
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Take(MaxGroups)
            .Select(g => new UrlGroup(g.Key, g.Count(), g.Take(SamplesPerGroup).Select(u => u.AbsoluteUri).ToList()))
            .ToList();

    private static string PatternOf(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return "/";
        if (segments.Length > 1) return $"/{segments[0]}/**";
        var slug = segments[0].ToLowerInvariant();
        var dot = slug.LastIndexOf('.');
        if (dot > 0) return $"/*{slug[dot..]}";
        // Birçok altyapı tipi slug'a tek harfle yazar: "...-p-hnkk-1003726" ürün, "lastik-c-283608" kategori. Ürün adında da
        // tek harfli kelime olabildiği için ("verde-a-s-p-prll-1862400") sona en yakın işaret alınır.
        return TypeMarker().Match(slug) is { Success: true } m ? $"/*-{m.Groups[1].Value}-*" : "/*";
    }

    [GeneratedRegex(@"^.*-([a-z])-(?:[a-z0-9]+-)*\d+$")]
    private static partial Regex TypeMarker();
}
