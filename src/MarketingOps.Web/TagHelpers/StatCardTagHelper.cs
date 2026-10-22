using System.Net;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MarketingOps.Web.TagHelpers;

/// <summary>
/// Basit sayısal kart. İç içerik değerin altında gösterilir (ör. önem dağılımı).
/// &lt;stat-card label="Açık görevler" value="24" delta="-3" delta-good="down" href="..." /&gt;
/// </summary>
[HtmlTargetElement("stat-card")]
public class StatCardTagHelper : TagHelper
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Unit { get; set; }
    public string? Hint { get; set; }
    public int? Delta { get; set; }

    /// <summary>Değişimin hangi yönü iyi: "up" ya da "down".</summary>
    public string DeltaGood { get; set; } = "up";
    public string? DeltaLabel { get; set; } = "önceki döneme göre";
    public string? Href { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var child = (await output.GetChildContentAsync()).GetContent();
        output.TagName = Href is null ? "div" : "a";
        output.TagMode = TagMode.StartTagAndEndTag;
        var extra = output.Attributes.TryGetAttribute("class", out var c) ? " " + c.Value : "";
        output.Attributes.SetAttribute("class", "card flex flex-col gap-1 p-4" + (Href is null ? "" : " transition-colors duration-150 hover:border-line-strong cursor-pointer") + extra);
        if (Href is not null) output.Attributes.SetAttribute("href", Href);

        output.Content.SetHtmlContent(
            $"""
            <span class="eyebrow">{WebUtility.HtmlEncode(Label)}</span>
            <span class="flex items-baseline gap-1"><span class="tabular font-mono text-[26px] font-medium leading-tight tracking-[-0.02em] text-fg">{WebUtility.HtmlEncode(Value)}</span>{(Unit is null ? "" : $"<span class=\"text-sm text-fg-muted\">{WebUtility.HtmlEncode(Unit)}</span>")}</span>
            {DeltaHtml(Delta, DeltaGood, DeltaLabel)}
            {(Hint is null ? "" : $"<span class=\"text-xs text-fg-muted\">{WebUtility.HtmlEncode(Hint)}</span>")}
            {child}
            """);
    }

    /// <summary>Değişim göstergesi: ok yönü + işaretli sayı + açıklama; renk yalnızca destekleyicidir.</summary>
    public static string DeltaHtml(int? delta, string good = "up", string? label = null)
    {
        if (delta is null) return "";
        var d = delta.Value;
        var isGood = d == 0 ? (bool?)null : (d > 0) == (good == "up");
        var tone = isGood switch { true => "text-ok", false => "text-critical", _ => "text-fg-muted" };
        var icon = d > 0 ? "trending-up" : d < 0 ? "trending-down" : "minus";
        var text = Infrastructure.Format.Signed(d);
        var sr = isGood switch { true => "iyileşme", false => "kötüleşme", _ => "değişim yok" };
        return $"<span class=\"inline-flex items-center gap-1 text-xs {tone}\">{IconTagHelper.Svg(icon, "h-3.5 w-3.5")}<span class=\"tabular font-medium\">{text}</span><span class=\"sr-only\">({sr})</span>{(label is null ? "" : $"<span class=\"text-fg-muted\">{WebUtility.HtmlEncode(label)}</span>")}</span>";
    }
}
