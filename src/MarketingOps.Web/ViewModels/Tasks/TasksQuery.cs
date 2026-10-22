using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.ViewModels.Tasks;

/// <summary>Görev listesinin query string'i. <see cref="Board"/>: kanban görünümü (durum filtresi sütunlara dönüşür).</summary>
public sealed class TasksQuery
{
    [FromQuery(Name = "status")] public List<WorkTaskStatus> Statuses { get; set; } = [];
    [FromQuery(Name = "team")] public List<Team> Teams { get; set; } = [];
    [FromQuery(Name = "severity")] public List<Severity> Severities { get; set; } = [];
    [FromQuery(Name = "q")] public string? Q { get; set; }
    [FromQuery(Name = "view")] public string? View { get; set; }
    [FromQuery(Name = "page")] public int Page { get; set; } = 1;
    [FromQuery(Name = "pageSize")] public int PageSize { get; set; } = 25;

    public bool Board => string.Equals(View, "board", StringComparison.OrdinalIgnoreCase);
    public string? Search => string.IsNullOrWhiteSpace(Q) ? null : Q.Trim();
    public bool IsDefault => Statuses.Count == 0 && Teams.Count == 0 && Severities.Count == 0 && Search is null;
}
