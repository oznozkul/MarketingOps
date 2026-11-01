using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Rendering.Queries.GetRenderScreenshot;

public sealed class GetRenderScreenshotQueryHandler(IAppDbContext db) : IRequestHandler<GetRenderScreenshotQuery, RenderScreenshotResponse>
{
    public async Task<RenderScreenshotResponse> Handle(GetRenderScreenshotQuery request, CancellationToken cancellationToken)
    {
        return await db.RenderScreenshots.AsNoTracking()
            .Where(s => s.Id == request.ScreenshotId && s.SiteId == request.SiteId)
            .Select(s => new RenderScreenshotResponse("image/jpeg", s.Bytes))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Ekran görüntüsü bulunamadı.");
    }
}
