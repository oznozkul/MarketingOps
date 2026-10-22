using MarketingOps.Application.Findings.Commands.BulkUpdateFindings;
using MarketingOps.Application.Findings.Commands.MarkFindingFalsePositive;
using MarketingOps.Application.Findings.Commands.RevertFalsePositive;
using MarketingOps.Application.Findings.Commands.SnoozeFinding;
using MarketingOps.Application.Findings.Queries;
using MarketingOps.Application.Findings.Queries.ExportFindingOccurrences;
using MarketingOps.Application.Findings.Queries.ExportFindings;
using MarketingOps.Application.Findings.Queries.GetFindingById;
using MarketingOps.Application.Findings.Queries.GetFindings;
using MarketingOps.Application.Findings.Queries.GetFindingSummary;
using MarketingOps.Application.Metrics.Queries.GetSearchPerformance;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Application.Tasks.Commands.CreateTasks;
using MarketingOps.Application.Tasks.Queries.GetTaskById;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;
using MarketingOps.Web.Filters;
using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewComponents;
using MarketingOps.Web.ViewModels;
using MarketingOps.Web.ViewModels.Findings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}/findings")]
public class FindingsController(ISender sender) : SiteControllerBase
{
    private const string FilterForm = "findings-filter";

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid siteId, FindingsQuery query, CancellationToken ct)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = PaginationViewComponent.Sizes.Contains(query.PageSize) ? query.PageSize : PaginationViewComponent.Sizes[0];
        var filter = query.ToFilter();

        var findings = await sender.Send(new GetFindingsQuery(siteId, filter, query.Sort, query.Desc, query.Page, query.PageSize), ct);
        var summary = await sender.Send(new GetFindingSummaryQuery(siteId, filter), ct);
        var pageTypes = (await sender.Send(new GetSiteByIdQuery(siteId), ct)).PageTypes;

        var model = new FindingsIndexViewModel(query, summary, findings, Menus(query, pageTypes), Chips(query, pageTypes));
        // Tablo bölgesi (filtre, sıralama, sayfalama) yalnızca kendini yeniler; chip'ler formu da güncellemek için sayfanın tamamını ister.
        if (Request.IsHtmxPartial() && Request.Headers["HX-Target"] == "findings-results") return PartialView("_FindingsResults", model);

        ViewData["Title"] = "Bulgular";
        return View(model);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(Guid siteId, FindingsQuery query, CancellationToken ct)
    {
        var rows = await sender.Send(new ExportFindingsQuery(siteId, query.ToFilter(), query.Sort, query.Desc), ct);
        return CsvFile.Create($"bulgular-{DateTime.UtcNow:yyyyMMdd}.csv",
            ["Önem", "Bulgu", "Kural", "Kategori", "Sayfa tipi", "Etkilenen sayfa", "İlk tespit", "Son görülme", "Durum", "Görev"],
            rows.Select(f => new[]
            {
                DisplayText.Of(f.Severity), f.Title, f.RuleCode, DisplayText.Of(f.Category), f.PageTypeName ?? "Eşleşmeyen",
                f.AffectedCount.ToString(), Format.DateTime(f.FirstSeenAt), Format.DateTime(f.LastSeenAt), DisplayText.Of(f.Status), f.TaskKey
            }));
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> Bulk(Guid siteId, BulkFindingsForm form)
    {
        var (ok, result) = await TrySendAsync(sender,
            new BulkUpdateFindingsCommand(siteId, form.Ids, form.Action, form.Reason, form.Until, CurrentUser.Name));
        if (ok)
        {
            var verb = form.Action == BulkFindingAction.Snooze ? "ertelendi" : "yanlış alarm olarak işaretlendi";
            var message = $"{result!.Updated} bulgu {verb}." + (result.Skipped > 0 ? $" Durumu uygun olmayan {result.Skipped} bulgu atlandı." : "");
            TempData[FlashMessage.Key] = message;
            Response.Headers["HX-Refresh"] = "true";
            return NoContent();
        }
        return PartialView(form.Action == BulkFindingAction.Snooze ? "_BulkSnoozeForm" : "_BulkFalsePositiveForm", form);
    }

    /// <summary>Seçili bulgular için eksik görevleri oluşturur.</summary>
    [HttpPost("bulk-tasks")]
    public async Task<IActionResult> BulkCreateTasks(Guid siteId, List<Guid> ids)
    {
        var (ok, created) = await TrySendAsync(sender, new CreateTasksCommand(siteId, ids));
        if (!ok) return ErrorToast();
        var skipped = ids.Count - created;
        return Refresh($"{created} görev oluşturuldu." + (skipped > 0 ? $" {skipped} bulgunun görevi zaten vardı." : ""));
    }

    [HttpGet("{id:guid}")]
    [SiteSwitchFallback(nameof(Index))]
    public async Task<IActionResult> Detail(Guid siteId, Guid id, string? q, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        pageSize = PaginationViewComponent.Sizes.Contains(pageSize) ? pageSize : PaginationViewComponent.Sizes[0];
        var search = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var detail = await sender.Send(new GetFindingByIdQuery(siteId, id, search, Math.Max(1, page), pageSize), ct);
        if (Request.IsHtmxPartial()) return PartialView("_OccurrencesTable", new FindingDetailViewModel(detail, search, null));

        var task = detail.Item.TaskId is { } taskId ? await sender.Send(new GetTaskByIdQuery(siteId, taskId), ct) : null;
        var model = new FindingDetailViewModel(detail, search, task, await sender.Send(new GetSearchPerformanceQuery(siteId, id), ct));

        ViewData["Title"] = detail.Item.Title;
        return View(model);
    }

    [HttpGet("{id:guid}/occurrences.csv")]
    public async Task<IActionResult> ExportOccurrences(Guid siteId, Guid id, CancellationToken ct)
    {
        var rows = await sender.Send(new ExportFindingOccurrencesQuery(siteId, id), ct);
        return CsvFile.Create($"bulgu-{id.ToString()[..8]}-sayfalar.csv", ["URL", "Mevcut değer", "Ayrıntı"],
            rows.Select(o => new[] { o.Url, o.CurrentValue, o.Detail }));
    }

    [HttpPost("{id:guid}/false-positive")]
    public async Task<IActionResult> MarkFalsePositive(Guid siteId, Guid id, FalsePositiveForm form)
    {
        if (await TrySendAsync(sender, new MarkFindingFalsePositiveCommand(siteId, id, form.Reason ?? "", CurrentUser.Name)))
            return Refresh("Yanlış alarm olarak işaretlendi. Kural bu sayfa tipi için susturuldu.");
        return PartialView("_FalsePositiveForm", form);
    }

    [HttpPost("{id:guid}/revert-false-positive")]
    public async Task<IActionResult> RevertFalsePositive(Guid siteId, Guid id)
    {
        if (await TrySendAsync(sender, new RevertFalsePositiveCommand(siteId, id, CurrentUser.Name)))
            return Refresh("Yanlış alarm işareti geri alındı.");
        Response.Toast(ModelState.FirstError(string.Empty) ?? "İşlem yapılamadı.", "error");
        return NoContent();
    }

    [HttpPost("{id:guid}/snooze")]
    public async Task<IActionResult> Snooze(Guid siteId, Guid id, SnoozeForm form)
    {
        if (form.Until is null) ModelState.AddModelError(nameof(SnoozeForm.Until), "Erteleme tarihi zorunludur.");
        else if (await TrySendAsync(sender, new SnoozeFindingCommand(siteId, id, form.Until.Value, CurrentUser.Name)))
            return Refresh($"Bulgu {form.Until.Value.ToString("d MMMM yyyy", Format.Tr)} tarihine ertelendi.");
        return PartialView("_SnoozeForm", form);
    }

    private NoContentResult ErrorToast()
    {
        Response.Toast(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "İşlem yapılamadı.", "error");
        return NoContent();
    }

    private NoContentResult Refresh(string message)
    {
        TempData[FlashMessage.Key] = message;
        Response.Headers["HX-Refresh"] = "true";
        return NoContent();
    }

    private IReadOnlyList<FilterMenuViewModel> Menus(FindingsQuery query, IReadOnlyList<PageTypeResponse> pageTypes) =>
    [
        new("f-severity", "Önem", "severity",
            Enum.GetValues<Severity>().Select(s => new FilterOption(s.ToString(), DisplayText.Of(s), query.Severities.Contains(s))).ToList(), FilterForm),
        new("f-category", "Kategori", "category",
            Enum.GetValues<FindingCategory>().Select(c => new FilterOption(c.ToString(), DisplayText.Of(c), query.Categories.Contains(c))).ToList(), FilterForm),
        new("f-status", "Durum", "status",
            Enum.GetValues<FindingStatus>().Select(s => new FilterOption(s.ToString(), DisplayText.Of(s), query.Statuses.Contains(s))).ToList(), FilterForm),
        new("f-page-type", "Sayfa tipi", "pageType",
            pageTypes.Select(p => new FilterOption(p.Id.ToString(), p.Name, query.PageTypeIds.Contains(p.Id)))
                .Append(new FilterOption(FindingsQuery.Unmatched, "Eşleşmeyen sayfalar", query.IncludeUnmatched)).ToList(), FilterForm),
        new("f-rule", "Kural", "rule",
            RuleCatalog.All.OrderBy(r => r.Name, StringComparer.Create(Format.Tr, false))
                .Select(r => new FilterOption(r.Code, r.Name, query.Rules.Contains(r.Code))).ToList(), FilterForm)
    ];

    private List<ChipViewModel> Chips(FindingsQuery query, IReadOnlyList<PageTypeResponse> pageTypes)
    {
        var chips = new List<ChipViewModel>();
        void AddList(string group, string key, IReadOnlyList<string> values, Func<string, string> label, Func<IReadOnlyList<string>, IReadOnlyList<string>>? whenEmpty = null)
        {
            foreach (var value in values)
            {
                var rest = values.Where(v => v != value).ToList();
                chips.Add(new ChipViewModel(group, label(value), Request.WithList(key, rest.Count == 0 && whenEmpty is not null ? whenEmpty(rest) : rest)));
            }
        }

        AddList("Önem", "severity", query.Severities.Select(s => s.ToString()).ToList(), v => DisplayText.Of(Enum.Parse<Severity>(v)));
        AddList("Kategori", "category", query.Categories.Select(c => c.ToString()).ToList(), v => DisplayText.Of(Enum.Parse<FindingCategory>(v)));
        // Varsayılan durumlar chip olarak gösterilmez; son durum kaldırılınca tüm durumlar listelenir.
        if (!query.HasDefaultStatuses)
            AddList("Durum", "status", query.Statuses.Select(s => s.ToString()).ToList(), v => DisplayText.Of(Enum.Parse<FindingStatus>(v)), _ => [FindingsQuery.AnyStatus]);
        var names = pageTypes.ToDictionary(p => p.Id.ToString(), p => p.Name);
        AddList("Sayfa tipi", "pageType", query.PageTypes.Where(v => v == FindingsQuery.Unmatched || names.ContainsKey(v)).ToList(),
            v => names.GetValueOrDefault(v) ?? "Eşleşmeyen sayfalar");
        AddList("Kural", "rule", query.Rules.Where(c => RuleCatalog.Find(c) is not null).ToList(), v => RuleCatalog.Find(v)!.Name);
        if (query.NewOnly) chips.Add(new ChipViewModel("Filtre", "Son taramada yeni", Request.With(("new", null), ("page", null))));
        if (query.Search is { } search) chips.Add(new ChipViewModel("Arama", search, Request.With(("q", null), ("page", null))));
        return chips;
    }
}
