using System.Net;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MarketingOps.Web.TagHelpers;

/// <summary>
/// Hata durumu. retry-url verilirse "Tekrar dene" bağlantısı; hx-target verilirse yalnızca o bölge yeniden yüklenir.
/// </summary>
[HtmlTargetElement("error-state", TagStructure = TagStructure.WithoutEndTag)]
public class ErrorStateTagHelper : TagHelper
{
    public string Title { get; set; } = "Veriler yüklenemedi";
    public string? Description { get; set; } = "Veri kaynağına şu anda ulaşılamıyor. Birazdan tekrar deneyin.";
    public string? RetryUrl { get; set; }

    [HtmlAttributeName("retry-target")]
    public string? RetryTarget { get; set; }

    public bool Compact { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("role", "alert");
        var extra = output.Attributes.TryGetAttribute("class", out var c) ? " " + c.Value : "";
        output.Attributes.SetAttribute("class", $"flex flex-col items-center justify-center text-center {(Compact ? "px-4 py-8" : "px-6 py-14")}{extra}");

        var retry = "";
        if (RetryUrl is not null)
        {
            var url = WebUtility.HtmlEncode(RetryUrl);
            retry = RetryTarget is null
                ? $"<a href=\"{url}\" class=\"btn btn-secondary btn-sm mt-4\">{IconTagHelper.Svg("refresh-cw")}Tekrar dene</a>"
                : $"<button type=\"button\" hx-get=\"{url}\" hx-target=\"{WebUtility.HtmlEncode(RetryTarget)}\" hx-swap=\"outerHTML\" class=\"btn btn-secondary btn-sm mt-4\">{IconTagHelper.Svg("refresh-cw")}Tekrar dene</button>";
        }

        output.Content.SetHtmlContent(
            $"""
            <div class="icon-box border-critical-line bg-critical-soft text-critical">{IconTagHelper.Svg("circle-x", "h-5 w-5")}</div>
            <p class="text-sm font-semibold text-fg">{WebUtility.HtmlEncode(Title)}</p>
            {(Description is null ? "" : $"<p class=\"mt-1 max-w-md text-sm text-fg-muted\">{WebUtility.HtmlEncode(Description)}</p>")}
            {retry}
            """);
    }
}
