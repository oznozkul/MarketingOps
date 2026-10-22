using MarketingOps.Domain.Notifications;
using MediatR;

namespace MarketingOps.Application.Notifications.Queries.GetReportPreview;

public sealed record GetReportPreviewQuery(Guid SiteId, ReportPeriod Period) : IRequest<string>;
