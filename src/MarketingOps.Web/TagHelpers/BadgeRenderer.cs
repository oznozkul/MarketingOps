using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MarketingOps.Web.TagHelpers;

internal static class BadgeRenderer
{
    public static void Render(TagHelperOutput output, Tone tone, string icon, string text, string? title = null)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        var extra = output.Attributes.TryGetAttribute("class", out var c) ? " " + c.Value : "";
        output.Attributes.SetAttribute("class", $"badge {ToneClasses.Badge(tone)}{extra}");
        if (title is not null) output.Attributes.SetAttribute("title", title);
        output.Content.SetHtmlContent(IconTagHelper.Svg(icon) + "<span>" + System.Net.WebUtility.HtmlEncode(text) + "</span>");
    }
}
