using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using MarketingOps.Domain.Crawls;

namespace MarketingOps.Infrastructure.Crawling;

/// <summary>
/// Erişilebilirlik ve işaretleme sorunlarının HTML'den görülebilen kısmı: link adları ve hedefleri, başlık sırası, form
/// etiketleri, tablolar, iframe'ler, sayfa bölgeleri. Ziyaretçiye gösterilmeyen öğeler (hidden, aria-hidden, noscript,
/// template) sayılmaz. CSS ile gizlenen öğe HTML'den bilinemez; onu render denetimi görür.
/// </summary>
public static partial class MarkupExtractor
{
    private const string Excluded = "[hidden], [aria-hidden='true'], noscript, template";
    private const int ShortTextLength = 40;
    private const int MaxShortTexts = 100;
    private const int MaxHeadings = 300;
    private const int SampleLength = 160;
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly string[] NonFieldInputTypes = ["hidden", "submit", "button", "reset", "image"];

    public static LinkFacts Links(IDocument document)
    {
        var links = Visible(document, "a").ToList();
        return new LinkFacts
        {
            NoText = Issue(links.Where(a => !HasAccessibleName(a))),
            EmptyHref = Issue(links.Where(HasNoDestination)),
            BlankWithoutNoopener = Issue(links.Where(a =>
                string.Equals(a.GetAttribute("target"), "_blank", StringComparison.OrdinalIgnoreCase)
                && !RelRegex().IsMatch(a.GetAttribute("rel") ?? ""))),
            ShortTexts = links
                .Select(a => Collapse(a.TextContent).ToLower(Tr))
                .Where(t => t.Length is > 0 and <= ShortTextLength)
                .GroupBy(t => t)
                .Select(g => new LinkTextCount(g.Key, g.Count()))
                .OrderByDescending(t => t.Count)
                .ThenBy(t => t.Text, StringComparer.Ordinal)
                .Take(MaxShortTexts)
                .ToList()
        };
    }

    public static HeadingFacts Headings(IDocument document)
    {
        var headings = Visible(document, "h1, h2, h3, h4, h5, h6, [role='heading'][aria-level]").Take(MaxHeadings).ToList();
        return new HeadingFacts
        {
            Levels = headings.Select(Level).Where(l => l is >= 1 and <= 6).ToList(),
            Empty = Issue(headings.Where(h => Collapse(h.TextContent).Length == 0 && !HasImageText(h)))
        };
    }

    public static FormFacts Forms(IDocument document)
    {
        var fields = Visible(document, "input, select, textarea")
            .Where(f => !NonFieldInputTypes.Contains((f.GetAttribute("type") ?? "").Trim().ToLowerInvariant()))
            .ToList();
        var labelled = document.QuerySelectorAll("label[for]").Select(l => l.GetAttribute("for")!).ToHashSet(StringComparer.Ordinal);
        return new FormFacts
        {
            Fields = fields.Count,
            Unlabeled = Issue(fields.Where(f => !IsLabelled(f, labelled)))
        };
    }

    public static TableFacts Tables(IDocument document)
    {
        var tables = Visible(document, "table").Where(t => t.GetAttribute("role")?.Trim().ToLowerInvariant() is not ("presentation" or "none")).ToList();
        return new TableFacts
        {
            Tables = tables.Count,
            WithoutHeader = Issue(tables.Where(t => t.QuerySelector("th, [role='columnheader'], [role='rowheader']") is null))
        };
    }

    public static IframeFacts Iframes(IDocument document)
    {
        var frames = Visible(document, "iframe").ToList();
        return new IframeFacts
        {
            Iframes = frames.Count,
            WithoutTitle = Issue(frames.Where(f => string.IsNullOrWhiteSpace(f.GetAttribute("title")))),
            NotLazy = Issue(frames.Where(f => !string.Equals(f.GetAttribute("loading")?.Trim(), "lazy", StringComparison.OrdinalIgnoreCase)))
        };
    }

    public static LandmarkFacts Landmarks(IDocument document)
    {
        var navs = Visible(document, "nav, [role='navigation']").ToList();
        return new LandmarkFacts
        {
            Main = Visible(document, "main, [role='main']").Count(),
            Header = Visible(document, "header, [role='banner']").Count(),
            Nav = navs.Count,
            Footer = Visible(document, "footer, [role='contentinfo']").Count(),
            UnlabeledNavs = navs.Count > 1
                ? Issue(navs.Where(n => string.IsNullOrWhiteSpace(n.GetAttribute("aria-label")) && string.IsNullOrWhiteSpace(n.GetAttribute("aria-labelledby"))))
                : ElementIssue.None
        };
    }

    private static IEnumerable<IElement> Visible(IDocument document, string selector) =>
        document.QuerySelectorAll(selector).Where(e => e.Closest(Excluded) is null);

    /// <summary>Ekran okuyucunun okuyacağı bir ad var mı: aria-labelledby, aria-label, metin, alt metinli görsel ya da title.</summary>
    private static bool HasAccessibleName(IElement element)
    {
        var labelledBy = element.GetAttribute("aria-labelledby");
        if (!string.IsNullOrWhiteSpace(labelledBy)
            && labelledBy.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(id => Collapse(element.Owner?.GetElementById(id)?.TextContent ?? "").Length > 0))
            return true;
        return !string.IsNullOrWhiteSpace(element.GetAttribute("aria-label"))
               || Collapse(element.TextContent).Length > 0
               || HasImageText(element)
               || !string.IsNullOrWhiteSpace(element.GetAttribute("title"));
    }

    private static bool HasImageText(IElement element) =>
        element.QuerySelectorAll("img[alt]").Any(i => !string.IsNullOrWhiteSpace(i.GetAttribute("alt")));

    /// <summary>
    /// Hedefsiz link. İstisnalar: breadcrumb'ın son adımı (aria-current="page", href yok) ve buton gibi davranan
    /// link (role="button"); ikisi de bir adrese gitmek için değildir.
    /// </summary>
    private static bool HasNoDestination(IElement link)
    {
        var href = link.GetAttribute("href");
        if (href is null && link.GetAttribute("aria-current") == "page") return false;
        if (string.Equals(link.GetAttribute("role"), "button", StringComparison.OrdinalIgnoreCase)) return false;
        var value = href?.Trim() ?? "";
        return value.Length == 0 || value == "#" || value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLabelled(IElement field, HashSet<string> labelledIds)
    {
        if (!string.IsNullOrWhiteSpace(field.GetAttribute("aria-label"))) return true;
        if (!string.IsNullOrWhiteSpace(field.GetAttribute("aria-labelledby"))) return true;
        if (!string.IsNullOrWhiteSpace(field.GetAttribute("title"))) return true;
        var id = field.GetAttribute("id");
        if (id is not null && labelledIds.Contains(id)) return true;
        return field.Closest("label") is not null;
    }

    private static int Level(IElement heading) =>
        heading.LocalName is ['h', var digit] && char.IsDigit(digit)
            ? digit - '0'
            : int.TryParse(heading.GetAttribute("aria-level"), out var level) ? level : 0;

    private static ElementIssue Issue(IEnumerable<IElement> elements)
    {
        var list = elements.ToList();
        return list.Count == 0
            ? ElementIssue.None
            : new ElementIssue { Count = list.Count, Samples = list.Take(ElementIssue.MaxSamples).Select(Describe).ToList() };
    }

    /// <summary>Ekibin öğeyi kaynakta bulabileceği kısa tanım, ör. a[href="#"] "Sepete ekle".</summary>
    private static string Describe(IElement element)
    {
        var tag = element.LocalName;
        var key = element.Id is { Length: > 0 } id ? $"#{id}"
            : AttributeOf(element, "href") ?? AttributeOf(element, "src") ?? AttributeOf(element, "name") ?? AttributeOf(element, "type") ?? "";
        var text = Collapse(element.TextContent);
        var description = text.Length > 0 ? $"{tag}{key} \"{(text.Length > 40 ? text[..40] + "…" : text)}\"" : tag + key;
        return description.Length <= SampleLength ? description : description[..SampleLength] + "…";
    }

    private static string? AttributeOf(IElement element, string name) =>
        element.GetAttribute(name) is { } value ? $"[{name}=\"{(value.Length > 80 ? value[..80] + "…" : value)}\"]" : null;

    private static string Collapse(string text) => WhitespaceRegex().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\bno(opener|referrer)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RelRegex();
}
