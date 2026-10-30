namespace MarketingOps.Domain.Crawls;

/// <summary>Sayfanın paylaşım (Open Graph) etiketleri. Görsel boyut ve açıklaması Faz 8'de eklendi; eski kayıtlarda boştur.</summary>
public sealed record OpenGraphInfo(
    string? Title,
    string? Description,
    string? Image,
    string? Type,
    string? Url,
    string? ImageWidth = null,
    string? ImageHeight = null,
    string? ImageAlt = null);
