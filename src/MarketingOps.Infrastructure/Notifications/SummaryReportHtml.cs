using System.Net;
using System.Text;
using MarketingOps.Application.Notifications;

namespace MarketingOps.Infrastructure.Notifications;

/// <summary>Önizleme ve gönderilen posta aynı metni kullanır. Worker Razor barındırmaz.</summary>
public sealed class SummaryReportHtml : ISummaryReportRenderer
{
    public string Render(SummaryReport report)
    {
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html lang=\"tr\"><body style=\"margin:0;background:#f4f4f2;color:#1c1c1a;font-family:Arial,sans-serif;\">");
        html.Append("<div style=\"max-width:640px;margin:0 auto;padding:24px;\">");
        html.Append("<p style=\"font-size:12px;letter-spacing:.08em;text-transform:uppercase;color:#6b6b66;\">MarketingOps</p>");
        html.Append("<h1 style=\"font-size:22px;font-weight:600;margin:4px 0 16px;\">").Append(Encode(report.SiteName)).Append(" · ").Append(Encode(report.PeriodLabel)).Append("</h1>");

        html.Append("<table style=\"width:100%;border-collapse:collapse;background:#fff;border:1px solid #e4e4e0;\">");
        foreach (var score in report.Scores)
        {
            var delta = score.Delta is null ? "" : $" ({(score.Delta > 0 ? "+" : "")}{score.Delta})";
            html.Append("<tr><td style=\"padding:10px 12px;border-bottom:1px solid #e4e4e0;\">").Append(Encode(score.Name))
                .Append("</td><td style=\"padding:10px 12px;border-bottom:1px solid #e4e4e0;text-align:right;font-variant-numeric:tabular-nums;\">")
                .Append(score.Score).Append(Encode(delta)).Append("</td></tr>");
        }
        html.Append("</table>");

        html.Append("<h2 style=\"font-size:14px;margin:24px 0 8px;\">En önemli sorunlar</h2>");
        if (report.Issues.Count == 0) html.Append("<p style=\"color:#6b6b66;\">Açık bulgu yok.</p>");
        else
        {
            html.Append("<ol style=\"padding-left:18px;\">");
            foreach (var issue in report.Issues)
                html.Append("<li style=\"margin-bottom:6px;\"><strong>").Append(Encode(issue.Title)).Append("</strong> · ")
                    .Append(Encode(issue.Severity)).Append(" · ").Append(issue.Affected).Append(" sayfa</li>");
            html.Append("</ol>");
        }

        html.Append("<h2 style=\"font-size:14px;margin:24px 0 8px;\">Kapanan görevler</h2>");
        if (report.ClosedTasks.Count == 0) html.Append("<p style=\"color:#6b6b66;\">Bu dönemde kapanan görev yok.</p>");
        else
        {
            html.Append("<ul style=\"padding-left:18px;\">");
            foreach (var task in report.ClosedTasks)
                html.Append("<li>").Append(Encode(task.Key)).Append(" · ").Append(Encode(task.Title)).Append("</li>");
            html.Append("</ul>");
        }

        html.Append("<h2 style=\"font-size:14px;margin:24px 0 8px;\">Sipariş ve purchase</h2><p>").Append(Encode(report.OrderLine)).Append("</p>");
        html.Append("<p style=\"font-size:12px;color:#6b6b66;\">Bu ileti siteye yazmaz. Ödeme adımı simüle edilmez.</p>");
        html.Append("</div></body></html>");
        return html.ToString();
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? "");
}
