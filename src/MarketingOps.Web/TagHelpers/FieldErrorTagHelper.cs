using System.Net;
using MarketingOps.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MarketingOps.Web.TagHelpers;

/// <summary>
/// Alanın altındaki hata mesajı: &lt;field-error for="Name" /&gt;. Hata yoksa hiçbir şey yazmaz.
/// Girdi, aria-describedby ile "{for}-error" kimliğine bağlanmalıdır.
/// </summary>
[HtmlTargetElement("field-error", TagStructure = TagStructure.WithoutEndTag)]
public class FieldErrorTagHelper : TagHelper
{
    public string For { get; set; } = "";

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var message = ViewContext.ModelState.FirstError(For);
        if (message is null)
        {
            output.SuppressOutput();
            return;
        }
        output.TagName = "p";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("id", FieldIds.ErrorId(For));
        output.Attributes.SetAttribute("class", "mt-1 flex items-start gap-1 text-xs text-critical");
        output.Content.SetHtmlContent(IconTagHelper.Svg("circle-alert", "mt-px h-3.5 w-3.5 shrink-0") + "<span>" + WebUtility.HtmlEncode(message) + "</span>");
    }
}
