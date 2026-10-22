using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Tracking;
using MarketingOps.Domain.WorkTasks;
using MarketingOps.Web.Infrastructure;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MarketingOps.Web.TagHelpers;

/// <summary>
/// Durum rozeti: bulgu, görev, tarama, connector ve tag denetimi durumlarının tümü için tek bileşen.
/// &lt;status-badge value="Model.Status" /&gt;
/// </summary>
[HtmlTargetElement("status-badge", TagStructure = TagStructure.WithoutEndTag)]
public class StatusBadgeTagHelper : TagHelper
{
    public Enum? Value { get; set; }

    public static (Tone Tone, string Icon) Style(Enum value) => value switch
    {
        FindingStatus.Open => (Tone.Primary, "circle-dot"),
        FindingStatus.Reopened => (Tone.High, "rotate-ccw"),
        FindingStatus.Resolved => (Tone.Ok, "circle-check"),
        FindingStatus.FalsePositive => (Tone.Neutral, "bell-off"),
        FindingStatus.Snoozed => (Tone.Neutral, "clock"),

        WorkTaskStatus.Todo => (Tone.Neutral, "circle-dashed"),
        WorkTaskStatus.InProgress => (Tone.Primary, "loader-circle"),
        WorkTaskStatus.AwaitingVerification => (Tone.Warn, "hourglass"),
        WorkTaskStatus.Closed => (Tone.Ok, "circle-check"),

        CrawlStatus.Queued => (Tone.Neutral, "clock"),
        CrawlStatus.Running => (Tone.Primary, "loader-circle"),
        CrawlStatus.Completed => (Tone.Ok, "circle-check"),
        CrawlStatus.PartiallyCompleted => (Tone.Warn, "circle-alert"),
        CrawlStatus.Failed => (Tone.Critical, "circle-x"),
        CrawlStatus.Cancelled => (Tone.Neutral, "circle-minus"),

        ConnectorStatus.Connected => (Tone.Ok, "plug"),
        ConnectorStatus.NotConnected => (Tone.Neutral, "unplug"),
        ConnectorStatus.PendingApproval => (Tone.Warn, "hourglass"),
        ConnectorStatus.Expired => (Tone.Critical, "circle-x"),
        ConnectorStatus.Error => (Tone.Critical, "triangle-alert"),

        TagAuditStatus.Ok => (Tone.Ok, "check"),
        TagAuditStatus.Missing => (Tone.Critical, "circle-x"),
        TagAuditStatus.Duplicate => (Tone.High, "copy"),
        TagAuditStatus.WrongId => (Tone.High, "triangle-alert"),
        TagAuditStatus.FiresBeforeConsent => (Tone.Critical, "shield-check"),

        _ => (Tone.Neutral, "circle-dot")
    };

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Value is null)
        {
            output.SuppressOutput();
            return;
        }
        var (tone, icon) = Style(Value);
        BadgeRenderer.Render(output, tone, icon, DisplayText.Of(Value));
    }
}
