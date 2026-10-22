using MarketingOps.Application.Metrics.Queries.GetWebVitals;
using MarketingOps.Application.Rules.Queries.GetRules;
using MarketingOps.Domain.Metrics;

namespace MarketingOps.Web.ViewModels.Performance;

public sealed record PerformanceViewModel(DeviceStrategy Device, WebVitalsResponse Vitals, IReadOnlyList<RuleListItemResponse> Rules);
