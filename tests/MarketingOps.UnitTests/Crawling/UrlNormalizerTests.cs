using MarketingOps.Infrastructure.Crawling;

namespace MarketingOps.UnitTests.Crawling;

public class UrlNormalizerTests
{
    [Theory]
    [InlineData("https://Lastikborsasi.com/Urun/A#yorumlar", "https://lastikborsasi.com/Urun/A")]
    [InlineData("https://lastikborsasi.com/kategori/", "https://lastikborsasi.com/kategori")]
    [InlineData("https://lastikborsasi.com", "https://lastikborsasi.com/")]
    [InlineData("https://lastikborsasi.com:443/a", "https://lastikborsasi.com/a")]
    [InlineData("https://lastikborsasi.com/a?z=1&a=2", "https://lastikborsasi.com/a?a=2&z=1")]
    [InlineData("HTTP://LASTIKBORSASI.COM/a/?b=2&a=1#x", "http://lastikborsasi.com/a?a=1&b=2")]
    public void Normalizes(string input, string expected) =>
        UrlNormalizer.Normalize(new Uri(input)).ShouldBe(expected);

    [Theory]
    [InlineData("/urun/a", "https://a.com/urun/a")]
    [InlineData("../jant", "https://a.com/jant")]
    [InlineData("https://b.com/x", "https://b.com/x")]
    public void Resolves_relative_links(string href, string expected) =>
        UrlNormalizer.Resolve(new Uri("https://a.com/kategori/yaz"), href)!.AbsoluteUri.ShouldBe(expected);

    [Theory]
    [InlineData("mailto:a@b.com")]
    [InlineData("tel:+90")]
    [InlineData("javascript:void(0)")]
    [InlineData("")]
    public void Ignores_non_http_links(string href) =>
        UrlNormalizer.Resolve(new Uri("https://a.com/"), href).ShouldBeNull();
}
