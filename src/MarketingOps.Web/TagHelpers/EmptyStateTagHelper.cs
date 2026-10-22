using System.Net;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MarketingOps.Web.TagHelpers;

/// <summary>
/// Boş durum. İç içerik aksiyon alanı olarak gösterilir.
/// &lt;empty-state icon="inbox" title="..." description="..."&gt;&lt;a class="btn ..."&gt;...&lt;/a&gt;&lt;/empty-state&gt;
/// </summary>
[HtmlTargetElement("empty-state")]
public class EmptyStateTagHelper : TagHelper
{
    public string Icon { get; set; } = "inbox";
    public string Title { get; set; } = "";
    public string? Description { get; set; }

    /// <summary>"positive": her şey yolunda türü boş durum (ör. hiç bulgu yok).</summary>
    public string Variant { get; set; } = "neutral";

    public bool Compact { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var actions = (await output.GetChildContentAsync()).GetContent();
        var positive = Variant == "positive";
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        var extra = output.Attributes.TryGetAttribute("class", out var c) ? " " + c.Value : "";
        output.Attributes.SetAttribute("class", $"flex flex-col items-center justify-center text-center {(Compact ? "px-4 py-8" : "px-6 py-14")}{extra}");
        output.Content.SetHtmlContent(
            $"""
            <div class="icon-box {(positive ? "border-ok-line bg-ok-soft text-ok" : "border-line-strong bg-surface text-fg-muted")}">{IconTagHelper.Svg(positive ? "circle-check" : Icon, "h-5 w-5")}</div>
            <p class="text-sm font-semibold text-fg">{WebUtility.HtmlEncode(Title)}</p>
            {(Description is null ? "" : $"<p class=\"mt-1 max-w-md text-sm text-fg-muted\">{WebUtility.HtmlEncode(Description)}</p>")}
            {(string.IsNullOrWhiteSpace(actions) ? "" : $"<div class=\"mt-4 flex flex-wrap items-center justify-center gap-2\">{actions}</div>")}
            """);
    }
}
