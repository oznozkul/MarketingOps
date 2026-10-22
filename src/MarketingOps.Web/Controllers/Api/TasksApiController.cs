using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Tasks.Commands.AddTaskComment;
using MarketingOps.Application.Tasks.Commands.AssignTaskTeam;
using MarketingOps.Application.Tasks.Commands.ConfirmAcceptanceCriterion;
using MarketingOps.Application.Tasks.Commands.CreateTasks;
using MarketingOps.Application.Tasks.Commands.SubmitSuggestionFeedback;
using MarketingOps.Application.Tasks.Commands.UpdateTaskStatus;
using MarketingOps.Application.Tasks.Queries;
using MarketingOps.Application.Tasks.Queries.GetTaskById;
using MarketingOps.Application.Tasks.Queries.GetTasks;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;
using MarketingOps.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers.Api;

[ApiController]
[Route("api/sites/{siteId:guid}")]
public sealed class TasksApiController(ISender sender) : ControllerBase
{
    [HttpGet("tasks")]
    public Task<PagedResponse<TaskListItemResponse>> GetTasks(Guid siteId, [FromQuery] WorkTaskStatus[]? status, [FromQuery] Team[]? team,
        [FromQuery] Severity[]? severity, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        sender.Send(new GetTasksQuery(siteId, status, team, severity, search, page, pageSize), ct);

    [HttpGet("tasks/{taskId:guid}")]
    public Task<TaskDetailResponse> GetTask(Guid siteId, Guid taskId, CancellationToken ct) => sender.Send(new GetTaskByIdQuery(siteId, taskId), ct);

    /// <summary>Görevi olmayan bulgular için görev oluşturur (FindingIds boşsa tüm açık bulgular). Oluşturulan sayıyı döner.</summary>
    [HttpPost("tasks")]
    public Task<int> Create(Guid siteId, CreateTasksRequest request, CancellationToken ct) => sender.Send(new CreateTasksCommand(siteId, request.FindingIds), ct);

    [HttpPost("tasks/{taskId:guid}/status")]
    public async Task<IActionResult> Status(Guid siteId, Guid taskId, TaskStatusRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateTaskStatusCommand(siteId, taskId, request.Status, CurrentUser.Name), ct);
        return NoContent();
    }

    [HttpPost("tasks/{taskId:guid}/team")]
    public async Task<IActionResult> Team(Guid siteId, Guid taskId, TaskTeamRequest request, CancellationToken ct)
    {
        await sender.Send(new AssignTaskTeamCommand(siteId, taskId, request.Team, CurrentUser.Name), ct);
        return NoContent();
    }

    [HttpPost("tasks/{taskId:guid}/criteria/{criterionId:guid}")]
    public async Task<IActionResult> Criterion(Guid siteId, Guid taskId, Guid criterionId, CriterionRequest request, CancellationToken ct)
    {
        await sender.Send(new ConfirmAcceptanceCriterionCommand(siteId, taskId, criterionId, request.Confirmed, CurrentUser.Name), ct);
        return NoContent();
    }

    [HttpPost("tasks/{taskId:guid}/comments")]
    public async Task<IActionResult> Comment(Guid siteId, Guid taskId, TaskCommentRequest request, CancellationToken ct)
    {
        await sender.Send(new AddTaskCommentCommand(siteId, taskId, request.Text, CurrentUser.Name), ct);
        return NoContent();
    }

    [HttpPost("tasks/{taskId:guid}/feedback")]
    public async Task<IActionResult> Feedback(Guid siteId, Guid taskId, SuggestionFeedbackRequest request, CancellationToken ct)
    {
        await sender.Send(new SubmitSuggestionFeedbackCommand(siteId, taskId, request.IsHelpful, request.Note, CurrentUser.Name), ct);
        return NoContent();
    }

}
