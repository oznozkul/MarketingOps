using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Common.Organizations;

/// <summary>
/// Kimlik doğrulama gelene kadar tek bir organizasyon vardır. Auth fazında bu, oturumdaki kullanıcının
/// organizasyonuyla değiştirilecek.
/// </summary>
public static class DefaultOrganization
{
    public static async Task<Guid> GetIdAsync(IAppDbContext db, CancellationToken ct) =>
        await db.Organizations.AsNoTracking().OrderBy(o => o.Id).Select(o => (Guid?)o.Id).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Organizasyon bulunamadı.");
}
