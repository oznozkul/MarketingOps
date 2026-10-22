using MarketingOps.Domain.Common;
using MarketingOps.Domain.Sites;

namespace MarketingOps.UnitTests.Domain.Sites;

public class MatchConditionTests
{
    [Fact]
    public void Invalid_regex_is_rejected_on_creation()
    {
        var ex = Should.Throw<DomainException>(() => new MatchCondition(MatchConditionType.UrlRegex, "^/urun/(["));
        ex.Message.ShouldContain("Geçersiz düzenli ifade");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Empty_value_is_rejected(string value) =>
        Should.Throw<DomainException>(() => new MatchCondition(MatchConditionType.UrlGlob, value));

    [Fact]
    public void Value_is_trimmed() =>
        new MatchCondition(MatchConditionType.SchemaType, "  Product ").Value.ShouldBe("Product");
}
