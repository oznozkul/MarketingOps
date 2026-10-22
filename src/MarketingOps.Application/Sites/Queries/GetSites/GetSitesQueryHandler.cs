using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Sites.Queries.GetSites;

public sealed class GetSitesQueryHandler(IAppDbContext db) : IRequestHandler<GetSitesQuery, PagedResponse<SiteListItemResponse>>
{
    public async Task<PagedResponse<SiteListItemResponse>> Handle(GetSitesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Sites.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SiteListItemResponse(s.Id, s.Name, s.Domain, s.IsOwnershipVerified, s.PageTypes.Count))
            .ToListAsync(cancellationToken);

        return new PagedResponse<SiteListItemResponse>(items, request.Page, request.PageSize, total);
    }
}
