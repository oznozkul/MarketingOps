using MediatR;

namespace MarketingOps.Application.Notifications.Queries.GetReportPreview;

public sealed class GetReportPreviewQueryHandler(SummaryReportFactory reports, ISummaryReportRenderer renderer) : IRequestHandler<GetReportPreviewQuery, string>
{
    public async Task<string> Handle(GetReportPreviewQuery request, CancellationToken cancellationToken)
    {
        var report = await reports.BuildAsync(request.SiteId, request.Period, cancellationToken);
        return renderer.Render(report);
    }
}
