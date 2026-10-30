namespace MarketingOps.Domain.Crawls;

/// <summary>Linklerin erişilebilir adı, hedefi ve yeni sekme güvenliği.</summary>
public sealed record LinkFacts
{
    /// <summary>Metni, aria-label'ı, alt metinli görseli ve title'ı olmayan linkler.</summary>
    public ElementIssue NoText { get; init; } = ElementIssue.None;

    /// <summary>href'i yok, boş, "#" ya da javascript: olan linkler.</summary>
    public ElementIssue EmptyHref { get; init; } = ElementIssue.None;

    /// <summary>target="_blank" olup rel'inde noopener/noreferrer olmayan linkler.</summary>
    public ElementIssue BlankWithoutNoopener { get; init; } = ElementIssue.None;

    /// <summary>Kısa link metinleri (jenerik metin kontrolü için), en çok geçenden.</summary>
    public IReadOnlyList<LinkTextCount> ShortTexts { get; init; } = [];

    public static LinkFacts Empty { get; } = new();
}
