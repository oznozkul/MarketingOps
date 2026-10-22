using MarketingOps.Application.PageTypes;
using MarketingOps.Application.PageTypes.Commands.AddPageType;
using MarketingOps.Application.PageTypes.Queries.PreviewPageTypeMatch;
using MarketingOps.Domain.Sites;

namespace MarketingOps.UnitTests.Application.PageTypes;

public class PageTypeValidatorTests
{
    [Fact]
    public void Add_page_type_requires_conditions()
    {
        var result = new AddPageTypeCommandValidator().Validate(new AddPageTypeCommand(Guid.CreateVersion7(), "Ürün", MatchMode.Any, []));

        result.Errors.ShouldContain(e => e.PropertyName == "Conditions");
    }

    [Fact]
    public void Each_condition_value_is_required()
    {
        var result = new AddPageTypeCommandValidator().Validate(
            new AddPageTypeCommand(Guid.CreateVersion7(), "Ürün", MatchMode.Any, [new(MatchConditionType.UrlGlob, "")]));

        result.Errors.ShouldContain(e => e.PropertyName == "Conditions[0].Value");
    }

    [Fact]
    public void Preview_rejects_relative_urls()
    {
        var result = new PreviewPageTypeMatchQueryValidator().Validate(new PreviewPageTypeMatchQuery(
            MatchMode.Any, [new(MatchConditionType.UrlGlob, "/urun/*")], [new("/urun/abc")]));

        result.Errors.ShouldContain(e => e.PropertyName == "Samples[0].Url");
    }
}
