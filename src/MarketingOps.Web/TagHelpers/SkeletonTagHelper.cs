using System.Net;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MarketingOps.Web.TagHelpers;

/// <summary>Yükleniyor iskeleti: variant = table | cards | text | chart.</summary>
[HtmlTargetElement("skeleton", TagStructure = TagStructure.WithoutEndTag)]
public class SkeletonTagHelper : TagHelper
{
    public string Variant { get; set; } = "text";
    public int Rows { get; set; } = 3;
    public string Label { get; set; } = "Yükleniyor";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("role", "status");
        output.Attributes.SetAttribute("aria-live", "polite");
        var extra = output.Attributes.TryGetAttribute("class", out var c) ? " " + c.Value : "";
        output.Attributes.SetAttribute("class", "w-full" + extra);

        var sr = $"<span class=\"sr-only\">{WebUtility.HtmlEncode(Label)}…</span>";
        var body = Variant switch
        {
            "table" => "<div class=\"divide-y divide-line\">" +
                       "<div class=\"flex gap-4 bg-surface-2 px-3 py-2.5\"><div class=\"skeleton h-3 w-16\"></div><div class=\"skeleton h-3 w-48\"></div><div class=\"skeleton ml-auto h-3 w-20\"></div></div>" +
                       string.Concat(Enumerable.Range(0, Rows).Select(i =>
                           $"<div class=\"flex items-center gap-4 px-3 py-3\"><div class=\"skeleton h-5 w-16\"></div><div class=\"flex-1 space-y-1.5\"><div class=\"skeleton h-3\" style=\"width:{60 - i % 3 * 12}%\"></div><div class=\"skeleton h-2.5 w-32\"></div></div><div class=\"skeleton h-3 w-12\"></div><div class=\"skeleton hidden h-3 w-20 sm:block\"></div></div>")) +
                       "</div>",
            "cards" => "<div class=\"grid gap-4 sm:grid-cols-2 xl:grid-cols-" + Math.Min(Rows, 5) + "\">" +
                       string.Concat(Enumerable.Range(0, Rows).Select(_ =>
                           "<div class=\"card space-y-3 p-4\"><div class=\"skeleton h-3 w-24\"></div><div class=\"skeleton h-7 w-16\"></div><div class=\"skeleton h-2 w-full\"></div></div>")) +
                       "</div>",
            "chart" => "<div class=\"flex h-56 items-end gap-2 px-2\">" +
                       string.Concat(Enumerable.Range(0, 14).Select(i => $"<div class=\"skeleton flex-1\" style=\"height:{30 + (i * 37 % 60)}%\"></div>")) +
                       "</div>",
            _ => "<div class=\"space-y-2\">" +
                 string.Concat(Enumerable.Range(0, Rows).Select(i => $"<div class=\"skeleton h-3\" style=\"width:{95 - i % 4 * 15}%\"></div>")) +
                 "</div>"
        };
        output.Content.SetHtmlContent(sr + body);
    }
}
