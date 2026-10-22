using MarketingOps.Domain.Findings;
using MarketingOps.Web.Infrastructure;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MarketingOps.Web.TagHelpers;

/// <summary>&lt;severity-badge value="Severity.Critical" /&gt;</summary>
[HtmlTargetElement("severity-badge", TagStructure = TagStructure.WithoutEndTag)]
public class SeverityBadgeTagHelper : TagHelper
{
    public Severity Value { get; set; }

    public static Tone ToneOf(Severity s) => s switch
    {
        Severity.Critical => Tone.Critical,
        Severity.High => Tone.High,
        Severity.Medium => Tone.Medium,
        _ => Tone.Low
    };

    public static string IconOf(Severity s) => s switch
    {
        Severity.Critical => "octagon-alert",
        Severity.High => "triangle-alert",
        Severity.Medium => "circle-alert",
        _ => "circle-minus"
    };

    public override void Process(TagHelperContext context, TagHelperOutput output) =>
        BadgeRenderer.Render(output, ToneOf(Value), IconOf(Value), DisplayText.Of(Value));
}
