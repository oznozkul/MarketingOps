namespace MarketingOps.Web.TagHelpers;

public static class ToneClasses
{
    public static string Badge(Tone tone) => tone switch
    {
        Tone.Critical => "border-critical-line bg-critical-soft text-critical",
        Tone.High => "border-high-line bg-high-soft text-high",
        Tone.Medium => "border-medium-line bg-medium-soft text-medium",
        Tone.Low => "border-low-line bg-low-soft text-low",
        Tone.Ok => "border-ok-line bg-ok-soft text-ok",
        Tone.Warn => "border-warn-line bg-warn-soft text-warn",
        Tone.Primary => "border-fg/50 bg-surface text-fg",
        _ => "border-line-strong bg-surface text-fg-muted"
    };

    public static string Text(Tone tone) => tone switch
    {
        Tone.Critical => "text-critical",
        Tone.High => "text-high",
        Tone.Medium => "text-medium",
        Tone.Low => "text-low",
        Tone.Ok => "text-ok",
        Tone.Warn => "text-warn",
        Tone.Primary => "text-fg",
        _ => "text-fg-muted"
    };

    public static string Solid(Tone tone) => tone switch
    {
        Tone.Critical => "bg-critical-solid",
        Tone.High => "bg-high-solid",
        Tone.Medium => "bg-medium-solid",
        Tone.Low => "bg-low-solid",
        Tone.Ok => "bg-ok-solid",
        Tone.Warn => "bg-warn-solid",
        Tone.Primary => "bg-primary",
        _ => "bg-fg-subtle"
    };
}
