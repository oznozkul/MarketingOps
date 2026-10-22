using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Sites;

/// <summary>Command handler'larının ortak ihtiyacı: aggregate'i sayfa tipleriyle birlikte yükle, yoksa 404.</summary>
public static class SiteLoader
{
    public static async Task<Site> LoadAsync(this IAppDbContext db, Guid siteId, CancellationToken ct) =>
        await db.Sites.Include(s => s.PageTypes).FirstOrDefaultAsync(s => s.Id == siteId, ct)
        ?? throw new NotFoundException("Site bulunamadı.");
}
