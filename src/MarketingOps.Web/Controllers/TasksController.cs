using MarketingOps.Application.Tasks.Commands.AddTaskComment;
using MarketingOps.Application.Tasks.Commands.AssignTaskTeam;
using MarketingOps.Application.Tasks.Commands.ConfirmAcceptanceCriterion;
using MarketingOps.Application.Tasks.Commands.CreateTasks;
using MarketingOps.Application.Tasks.Commands.ExportTasks;
using MarketingOps.Application.Tasks.Commands.RequestTaskEnrichment;
using MarketingOps.Application.Tasks.Commands.SubmitSuggestionFeedback;
using MarketingOps.Application.Tasks.Commands.UpdateTaskStatus;
using MarketingOps.Application.Tasks.Queries.GetTaskById;
using MarketingOps.Application.Tasks.Queries.GetTasks;
using MarketingOps.Application.Tasks.Queries.GetUntaskedFindingCount;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;
using MarketingOps.Web.Filters;
using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewComponents;
using MarketingOps.Web.ViewModels;
using MarketingOps.Web.ViewModels.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

/// <summary>
/// Görevler. Görev detayı ayrı bir ekran değildir: bulgu detayının görev bölümüdür (bulgu başına tek görev).
/// Görev üzerindeki işlemler sayfayı yeniler; içerik birbirine bağlı (durum, kriterler, geçmiş) olduğu için tek bölge yenilemek yetmez.
/// </summary>
[Route("sites/{siteId:guid}/tasks")]
public class TasksController(ISender sender) : SiteControllerBase
{
    private const string FilterForm = "tasks-filter";
    private const int BoardColumnSize = 50;

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid siteId, TasksQuery query, CancellationToken ct)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = PaginationViewComponent.Sizes.Contains(query.PageSize) ? query.PageSize : PaginationViewComponent.Sizes[0];

        var columns = new List<TaskBoardColumn>();
        Application.Common.Models.PagedResponse<Application.Tasks.Queries.TaskListItemResponse>? table = null;
        if (query.Board)
        {
            foreach (var status in Enum.GetValues<WorkTaskStatus>())
                columns.Add(new TaskBoardColumn(status, await sender.Send(
                    new GetTasksQuery(siteId, [status], query.Teams, query.Severities, query.Search, 1, BoardColumnSize), ct)));
        }
        else
        {
            table = await sender.Send(new GetTasksQuery(siteId, query.Statuses, query.Teams, query.Severities, query.Search, query.Page, query.PageSize), ct);
        }

        var model = new TasksIndexViewModel(query, table, columns, await sender.Send(new GetUntaskedFindingCountQuery(siteId), ct), Menus(query), Chips(query));
        if (Request.IsHtmxPartial() && Request.Headers["HX-Target"] == "tasks-results") return PartialView("_TasksResults", model);
        ViewData["Title"] = "Görevler";
        return View(model);
    }

    /// <summary>Görev bağlantıları bulgu detayındaki görev bölümüne gider.</summary>
    [HttpGet("{id:guid}")]
    [SiteSwitchFallback(nameof(Index))]
    public async Task<IActionResult> Detail(Guid siteId, Guid id, CancellationToken ct)
    {
        var task = await sender.Send(new GetTaskByIdQuery(siteId, id), ct);
        return Redirect(Url.SiteAction("Detail", "Findings", new { id = task.FindingId }) + "#gorev");
    }

    /// <summary>Bulgu detayından tek bulgu için ya da görev listesinden tüm açık bulgular için görev oluşturur.</summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create(Guid siteId, Guid? findingId)
    {
        var (ok, created) = await TrySendAsync(sender, new CreateTasksCommand(siteId, findingId is { } id ? [id] : null));
        if (!ok) return ErrorToast();
        return Refresh(created switch
        {
            0 => "Oluşturulacak görev yok; açık bulguların hepsinin görevi var.",
            1 => "Görev oluşturuldu.",
            _ => $"{created} görev oluşturuldu."
        });
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid siteId, Guid id, WorkTaskStatus status) =>
        await TrySendAsync(sender, new UpdateTaskStatusCommand(siteId, id, status, CurrentUser.Name)) ? Refresh("Görev durumu güncellendi.") : ErrorToast();

    [HttpPost("{id:guid}/team")]
    public async Task<IActionResult> Team(Guid siteId, Guid id, Team team) =>
        await TrySendAsync(sender, new AssignTaskTeamCommand(siteId, id, team, CurrentUser.Name)) ? Refresh("Ekip güncellendi.") : ErrorToast();

    [HttpPost("{id:guid}/criteria/{criterionId:guid}")]
    public async Task<IActionResult> Criterion(Guid siteId, Guid id, Guid criterionId, bool confirmed) =>
        await TrySendAsync(sender, new ConfirmAcceptanceCriterionCommand(siteId, id, criterionId, confirmed, CurrentUser.Name))
            ? Refresh(confirmed ? "Kriter onaylandı." : "Kriter onayı geri alındı.")
            : ErrorToast();

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> Comment(Guid siteId, Guid id, TaskCommentForm form)
    {
        if (await TrySendAsync(sender, new AddTaskCommentCommand(siteId, id, form.Text ?? "", CurrentUser.Name))) return Refresh("Yorum eklendi.");
        ViewData["TaskId"] = id;
        return PartialView("~/Views/Findings/_TaskCommentForm.cshtml", form);
    }

    [HttpPost("{id:guid}/feedback")]
    public async Task<IActionResult> Feedback(Guid siteId, Guid id, SuggestionFeedbackForm form) =>
        await TrySendAsync(sender, new SubmitSuggestionFeedbackCommand(siteId, id, form.IsHelpful, form.Note, CurrentUser.Name))
            ? Refresh("Geri bildiriminiz kaydedildi. Teşekkürler.")
            : ErrorToast();

    [HttpPost("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid siteId, Guid id)
    {
        var (ok, result) = await TrySendAsync(sender, new ExportTasksCommand(siteId, [id]));
        if (!ok) return ErrorToast();
        if (result!.Errors.Count > 0)
        {
            Response.Toast(result.Errors[0], "error");
            return NoContent();
        }
        return Refresh("Görev Jira'ya aktarıldı.");
    }

    [HttpPost("{id:guid}/suggestion")]
    public async Task<IActionResult> RegenerateSuggestion(Guid siteId, Guid id) =>
        await TrySendAsync(sender, new RequestTaskEnrichmentCommand(siteId, id))
            ? Refresh("Öneri yeniden üretiliyor. Birkaç saniye sonra sayfayı yenileyin.")
            : ErrorToast();

    private NoContentResult Refresh(string message)
    {
        TempData[FlashMessage.Key] = message;
        Response.Headers["HX-Refresh"] = "true";
        return NoContent();
    }

    private NoContentResult ErrorToast()
    {
        Response.Toast(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "İşlem yapılamadı.", "error");
        return NoContent();
    }

    private static List<FilterMenuViewModel> Menus(TasksQuery query)
    {
        var menus = new List<FilterMenuViewModel>
        {
            new("t-team", "Ekip", "team", Enum.GetValues<Team>().Select(t => new FilterOption(t.ToString(), DisplayText.Of(t), query.Teams.Contains(t))).ToList(), FilterForm),
            new("t-severity", "Önem", "severity", Enum.GetValues<Severity>().Select(s => new FilterOption(s.ToString(), DisplayText.Of(s), query.Severities.Contains(s))).ToList(), FilterForm)
        };
        // Panoda durumlar sütundur; durum filtresi yalnızca tabloda anlamlı.
        if (!query.Board)
            menus.Insert(0, new("t-status", "Durum", "status",
                Enum.GetValues<WorkTaskStatus>().Select(s => new FilterOption(s.ToString(), DisplayText.Of(s), query.Statuses.Contains(s))).ToList(), FilterForm));
        return menus;
    }

    private List<ChipViewModel> Chips(TasksQuery query)
    {
        var chips = new List<ChipViewModel>();
        void Add<T>(string group, string key, List<T> values) where T : struct, Enum
        {
            foreach (var value in values)
                chips.Add(new ChipViewModel(group, DisplayText.Of(value), Request.WithList(key, values.Where(v => !v.Equals(value)).Select(v => v.ToString()))));
        }
        if (!query.Board) Add("Durum", "status", query.Statuses);
        Add("Ekip", "team", query.Teams);
        Add("Önem", "severity", query.Severities);
        if (query.Search is { } search) chips.Add(new ChipViewModel("Arama", search, Request.With(("q", null), ("page", null))));
        return chips;
    }
}
