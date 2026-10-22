using System.Net;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MarketingOps.Web.TagHelpers;

/// <summary>Formun geneline ait hatalar (domain kuralı, çakışma): &lt;form-errors /&gt;. role=alert ile okunur.</summary>
[HtmlTargetElement("form-errors", TagStructure = TagStructure.WithoutEndTag)]
public class FormErrorsTagHelper : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var messages = ViewContext.ModelState.TryGetValue(string.Empty, out var entry)
            ? entry.Errors.Select(e => e.ErrorMessage).Distinct().ToList()
            : [];
        if (messages.Count == 0)
        {
            output.SuppressOutput();
            return;
        }
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("role", "alert");
        output.Attributes.SetAttribute("class", "flex items-start gap-2 border-l-2 border-critical-solid bg-critical-soft px-3 py-2 text-sm text-critical");
        output.Content.SetHtmlContent(IconTagHelper.Svg("circle-alert", "mt-0.5 h-4 w-4 shrink-0") +
            "<div>" + string.Join("", messages.Select(m => $"<p>{WebUtility.HtmlEncode(m)}</p>")) + "</div>");
    }
}
