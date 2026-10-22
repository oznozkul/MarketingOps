using MarketingOps.Domain.Findings;
using MarketingOps.Web.Infrastructure;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MarketingOps.Web.TagHelpers;

/// <summary>&lt;category-badge value="FindingCategory.Tracking" /&gt; — kategori nötr renkte gösterilir.</summary>
[HtmlTargetElement("category-badge", TagStructure = TagStructure.WithoutEndTag)]
public class CategoryBadgeTagHelper : TagHelper
{
    public FindingCategory Value { get; set; }

    public static string IconOf(FindingCategory c) => c switch
    {
        FindingCategory.Tracking => "activity",
        FindingCategory.TechnicalSeo => "file-code",
        FindingCategory.Content => "list",
        _ => "gauge"
    };

    public override void Process(TagHelperContext context, TagHelperOutput output) =>
        BadgeRenderer.Render(output, Tone.Neutral, IconOf(Value), DisplayText.Of(Value));
}
