namespace MarketingOps.Infrastructure.Crawling;

/// <summary>
/// robots.txt kuralları. Kendi user-agent grubumuz varsa o, yoksa "*" grubu uygulanır. Allow/Disallow arasında
/// en uzun eşleşen kural kazanır, eşitlikte Allow (Google'ın uyguladığı davranış). "*" ve "$" desteklenir.
/// </summary>
public sealed class RobotsRules
{
    private readonly List<(string Pattern, bool Allow)> _rules;

    public IReadOnlyList<string> Sitemaps { get; }

    /// <summary>Site sahibinin istediği istekler arası bekleme (Crawl-delay); site ayarındaki hızdan yavaşsa bu uygulanır.</summary>
    public TimeSpan? CrawlDelay { get; }

    public static RobotsRules AllowAll { get; } = new([], [], null);

    private RobotsRules(List<(string Pattern, bool Allow)> rules, IReadOnlyList<string> sitemaps, TimeSpan? crawlDelay)
    {
        _rules = rules;
        Sitemaps = sitemaps;
        CrawlDelay = crawlDelay;
    }

    public static RobotsRules Parse(string content, string userAgentToken)
    {
        var groups = new List<(List<string> Agents, List<(string, bool)> Rules)>();
        var delays = new Dictionary<List<(string, bool)>, double>();
        var sitemaps = new List<string>();
        (List<string> Agents, List<(string, bool)> Rules)? current = null;
        var lastWasAgent = false;

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Split('#')[0].Trim();
            var colon = line.IndexOf(':');
            if (colon <= 0) continue;
            var key = line[..colon].Trim().ToLowerInvariant();
            var value = line[(colon + 1)..].Trim();

            switch (key)
            {
                case "user-agent":
                    if (current is null || !lastWasAgent)
                    {
                        current = ([], []);
                        groups.Add(current.Value);
                    }
                    current.Value.Agents.Add(value.ToLowerInvariant());
                    lastWasAgent = true;
                    continue;
                case "allow" or "disallow" when current is not null:
                    // Boş Disallow "her şeye izin" demektir; kural eklenmez.
                    if (value.Length > 0) current.Value.Rules.Add((value, key == "allow"));
                    break;
                case "crawl-delay" when current is not null:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
                        delays[current.Value.Rules] = seconds;
                    break;
                case "sitemap":
                    if (value.Length > 0) sitemaps.Add(value);
                    break;
            }
            lastWasAgent = false;
        }

        var token = userAgentToken.ToLowerInvariant();
        var ourGroups = groups.Where(g => g.Agents.Any(a => a != "*" && token.Contains(a))).ToList();
        var chosen = ourGroups.Count > 0 ? ourGroups : groups.Where(g => g.Agents.Contains("*")).ToList();
        var delay = chosen.Select(g => delays.TryGetValue(g.Rules, out var d) ? d : (double?)null).Max();
        return new RobotsRules(chosen.SelectMany(g => g.Rules).ToList(), sitemaps, delay is { } s ? TimeSpan.FromSeconds(Math.Min(s, 60)) : null);
    }

    public bool IsAllowed(Uri url)
    {
        var path = url.PathAndQuery;
        var best = _rules
            .Where(r => Matches(r.Pattern, path))
            .OrderByDescending(r => r.Pattern.Length)
            .ThenByDescending(r => r.Allow)
            .Select(r => (bool?)r.Allow)
            .FirstOrDefault();
        return best ?? true;
    }

    private static bool Matches(string pattern, string path)
    {
        var anchored = pattern.EndsWith('$');
        var body = anchored ? pattern[..^1] : pattern;
        var parts = body.Split('*');

        var position = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (i == 0)
            {
                if (!path.StartsWith(part, StringComparison.Ordinal)) return false;
                position = part.Length;
                continue;
            }
            var found = path.IndexOf(part, position, StringComparison.Ordinal);
            if (found < 0) return false;
            position = found + part.Length;
        }
        if (!anchored) return true;
        return parts.Length == 1 ? path.Length == body.Length : path.EndsWith(parts[^1], StringComparison.Ordinal) || parts[^1].Length == 0;
    }
}
