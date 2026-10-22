using MediatR;

namespace MarketingOps.Application.Tasks.Commands.CreateTasks;

/// <summary>
/// Seçili bulgular için (FindingIds boşsa sitenin tüm açık bulguları için) eksik görevleri oluşturur.
/// Görev üretiminden önce açılmış bulgular da bu yolla göreve dönüştürülür. Oluşturulan görev sayısını döner.
/// </summary>
public sealed record CreateTasksCommand(Guid SiteId, IReadOnlyList<Guid>? FindingIds = null) : IRequest<int>;
