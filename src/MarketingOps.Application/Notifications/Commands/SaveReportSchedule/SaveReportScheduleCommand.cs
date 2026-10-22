using MarketingOps.Domain.Notifications;
using MediatR;

namespace MarketingOps.Application.Notifications.Commands.SaveReportSchedule;

public sealed record SaveReportScheduleCommand(Guid SiteId, ReportPeriod Period, bool Enabled, TimeOnly SendAt, string? Recipients) : IRequest;
