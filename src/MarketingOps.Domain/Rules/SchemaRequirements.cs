using MarketingOps.Domain.Crawls;

namespace MarketingOps.Domain.Rules;

/// <summary>
/// Google zengin sonuçlarının bilinen tiplerde istediği alanlar. Her grup "bunlardan biri dolu olmalı" demektir.
/// Yalnızca üst düzey düğümler denetlenir; iç içe düğümün (ör. Article.publisher) bağlamı farklıdır.
/// Tabloda olmayan tipin alanı denetlenmez.
/// </summary>
public static class SchemaRequirements
{
    private static readonly string[][] Organization = [["name"], ["url"], ["logo"]];
    private static readonly string[][] Article = [["headline"], ["image"], ["datePublished"], ["author"]];

    private static readonly Dictionary<string, string[][]> ByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Product"] = [["name"], ["image"], ["offers", "review", "aggregateRating"]],
        // Ad ListItem'da ya da gösterdiği öğede (item.name) olabilir; ikisi de geçerlidir.
        ["BreadcrumbList"] = [["itemListElement"], ["itemListElement.position"], ["itemListElement.name", "itemListElement.item.name"]],
        ["Organization"] = Organization,
        ["Corporation"] = Organization,
        ["OnlineStore"] = Organization,
        ["OnlineBusiness"] = Organization,
        ["WebSite"] = [["name"], ["url"]],
        ["Article"] = Article,
        ["NewsArticle"] = Article,
        ["BlogPosting"] = Article,
        ["FAQPage"] = [["mainEntity"], ["mainEntity.name"], ["mainEntity.acceptedAnswer.text"]],
        ["ItemList"] = [["itemListElement"]],
        ["LocalBusiness"] = [["name"], ["address"]]
    };

    /// <summary>Teklif tek fiyatlıysa (Offer) fiyat, para birimi ve stok; fiyat aralığıysa (AggregateOffer) en düşük fiyat ve para birimi.</summary>
    private static readonly string[][] Offer =
        [["offers.price", "offers.priceSpecification.price"], ["offers.priceCurrency", "offers.priceSpecification.priceCurrency"], ["offers.availability"]];
    private static readonly string[][] AggregateOffer = [["offers.lowPrice"], ["offers.priceCurrency"]];

    /// <summary>Düğümün eksik alanları, ör. "offers.availability" ya da "offers | review | aggregateRating".</summary>
    public static IReadOnlyList<string> Missing(SchemaNode node)
    {
        if (!node.TopLevel || !ByType.TryGetValue(node.Type, out var groups)) return [];
        var properties = node.Properties.ToHashSet(StringComparer.Ordinal);
        IEnumerable<string[]> required = groups;
        if (node.Type.Equals("Product", StringComparison.OrdinalIgnoreCase) && properties.Contains("offers"))
            required = required.Concat(properties.Contains("offers.lowPrice") ? AggregateOffer : Offer);
        return required.Where(g => !g.Any(properties.Contains)).Select(g => string.Join(" | ", g)).ToList();
    }

    /// <summary>Sitenin kimliğini anlatan tipler: Organization ve alt tipleri.</summary>
    public static bool IsOrganization(string type) =>
        type is "Organization" or "Corporation" or "OnlineStore" or "OnlineBusiness" or "LocalBusiness" or "Store";
}
