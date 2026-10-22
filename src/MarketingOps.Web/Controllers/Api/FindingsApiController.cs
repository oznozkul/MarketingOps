using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Findings.Commands.BulkUpdateFindings;
using MarketingOps.Application.Findings.Commands.MarkFindingFalsePositive;
using MarketingOps.Application.Findings.Commands.RevertFalsePositive;
using MarketingOps.Application.Findings.Commands.SnoozeFinding;
using MarketingOps.Application.Findings.Queries;
using MarketingOps.Application.Findings.Queries.GetFindingById;
using MarketingOps.Application.Findings.Queries.GetFindings;
using MarketingOps.Application.Findings.Queries.GetFindingSummary;
using MarketingOps.Domain.Findings;
using MarketingOps.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers.Api;

[ApiController]
[Route("api/sites/{siteId:guid}/findings")]
public sealed class FindingsApiController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<PagedResponse<FindingListItemResponse>> GetFindings(Guid siteId,
        [FromQuery] Severity[]? severity, [FromQuery] FindingCategory[]? category, [FromQuery] FindingStatus[]? status,
        [FromQuery] Guid[]? pageTypeId, [FromQuery] bool unmatched, [FromQuery] string[]? ruleCode, [FromQuery] bool newInLastCrawl,
        [FromQuery] string? search, [FromQuery] FindingSort sort = FindingSort.Severity, [FromQuery] bool descending = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        sender.Send(new GetFindingsQuery(siteId,
            new FindingFilter(severity, category, status, pageTypeId, unmatched, ruleCode, newInLastCrawl, search), sort, descending, page, pageSize), ct);

    [HttpGet("summary")]
    public Task<FindingSummaryResponse> GetSummary(Guid siteId, CancellationToken ct) =>
        sender.Send(new GetFindingSummaryQuery(siteId, new FindingFilter()), ct);

    [HttpGet("{findingId:guid}")]
    public Task<FindingDetailResponse> GetFinding(Guid siteId, Guid findingId, [FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        sender.Send(new GetFindingByIdQuery(siteId, findingId, search, page, pageSize), ct);

    [HttpPost("{findingId:guid}/false-positive")]
    public async Task<IActionResult> MarkFalsePositive(Guid siteId, Guid findingId, FalsePositiveRequest request, CancellationToken ct)
    {
        await sender.Send(new MarkFindingFalsePositiveCommand(siteId, findingId, request.Reason, CurrentUser.Name), ct);
        return NoContent();
    }

    [HttpPost("{findingId:guid}/revert-false-positive")]
    public async Task<IActionResult> RevertFalsePositive(Guid siteId, Guid findingId, CancellationToken ct)
    {
        await sender.Send(new RevertFalsePositiveCommand(siteId, findingId, CurrentUser.Name), ct);
        return NoContent();
    }

    [HttpPost("{findingId:guid}/snooze")]
    public async Task<IActionResult> Snooze(Guid siteId, Guid findingId, SnoozeRequest request, CancellationToken ct)
    {
        await sender.Send(new SnoozeFindingCommand(siteId, findingId, request.Until, CurrentUser.Name), ct);
        return NoContent();
    }

    [HttpPost("bulk")]
    public Task<BulkUpdateResult> Bulk(Guid siteId, BulkFindingsRequest request, CancellationToken ct) =>
        sender.Send(new BulkUpdateFindingsCommand(siteId, request.FindingIds, request.Action, request.Reason, request.Until, CurrentUser.Name), ct);
}
