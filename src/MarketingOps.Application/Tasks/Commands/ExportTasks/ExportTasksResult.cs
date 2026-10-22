namespace MarketingOps.Application.Tasks.Commands.ExportTasks;

public sealed record ExportTasksResult(int Exported, IReadOnlyList<string> Errors);
