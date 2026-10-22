using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MarketingOps.Web.TagHelpers;

/// <summary>&lt;icon name="bell" class="h-4 w-4" /&gt; → satır içi Lucide SVG. Varsayılan olarak dekoratiftir (aria-hidden).</summary>
[HtmlTargetElement("icon", TagStructure = TagStructure.WithoutEndTag)]
public class IconTagHelper : TagHelper
{
    public string Name { get; set; } = "";

    /// <summary>Verilirse ikon anlam taşır ve ekran okuyucuya bu metin okunur.</summary>
    public string? Label { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("fill", "none");
        output.Attributes.SetAttribute("stroke", "currentColor");
        output.Attributes.SetAttribute("stroke-width", "2");
        output.Attributes.SetAttribute("stroke-linecap", "round");
        output.Attributes.SetAttribute("stroke-linejoin", "round");
        if (!output.Attributes.ContainsName("class"))
            output.Attributes.SetAttribute("class", "h-4 w-4 shrink-0");
        if (Label is null)
        {
            output.Attributes.SetAttribute("aria-hidden", "true");
            output.Attributes.SetAttribute("focusable", "false");
        }
        else
        {
            output.Attributes.SetAttribute("role", "img");
            output.Attributes.SetAttribute("aria-label", Label);
        }
        output.Content.SetHtmlContent(Icons.Paths.TryGetValue(Name, out var svg) ? svg : Icons.Paths["circle-help"]);
    }

    public static string Svg(string name, string cssClass = "h-4 w-4 shrink-0") =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" class=\"{cssClass}\" aria-hidden=\"true\" focusable=\"false\">{(Icons.Paths.TryGetValue(name, out var svg) ? svg : Icons.Paths["circle-help"])}</svg>";
}
