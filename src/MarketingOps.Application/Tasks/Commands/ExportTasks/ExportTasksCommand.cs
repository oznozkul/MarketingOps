using MediatR;

namespace MarketingOps.Application.Tasks.Commands.ExportTasks;

/// <summary>
/// Görevleri dış görev sistemine (Jira) aktarır. Daha önce aktarılmış görev için yeni kayıt açılmaz, mevcut kayıt güncellenir.
/// Bir görevin hatası diğerlerini durdurmaz. Görevler kimlikle ya da bağlı bulguyla (<see cref="FindingIds"/>, bulgu listesindeki toplu işlem) seçilir.
/// </summary>
public sealed record ExportTasksCommand(Guid SiteId, IReadOnlyList<Guid> TaskIds, IReadOnlyList<Guid>? FindingIds = null) : IRequest<ExportTasksResult>;
