using MarketingOps.Domain.Organizations;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Infrastructure.Persistence.Seed;

/// <summary>
/// Geliştirme ortamı için örnek veri. İdempotent: eksik olanı ekler, var olana dokunmaz;
/// bu yüzden her açılışta güvenle çalıştırılabilir.
/// </summary>
public sealed class DevelopmentSeeder(AppDbContext db, TimeProvider clock)
{
    // Sorgu dizesi olan adresler de eşleşsin diye regex'ler isteğe bağlı "?..." ile biter.
    private const string Query = @"(\?.*)?$";

    private static readonly (string Name, MatchMode Mode, MatchCondition[] Conditions) HomePage =
        ("Ana sayfa", MatchMode.Any, [new(MatchConditionType.UrlGlob, "/")]);

    // Ürün sayfası URL yapısı siteden siteye değişir; Product schema'sı her e-ticaret sitesinde ortak işarettir.
    private static readonly (string Name, MatchMode Mode, MatchCondition[] Conditions) ProductPage =
        ("Ürün", MatchMode.Any, [new(MatchConditionType.SchemaType, "Product")]);

    /// <summary>
    /// lastikborsasi.com'un desenleri gerçek tarama URL'lerinden çıkarıldı (ör. /185-65-r15-yaz-lastigi, /markalar/michelin/...-c-271462).
    /// tyresupply.com henüz taranmadığı için yalnızca her sitede geçerli tipleri alır.
    /// </summary>
    private static readonly (string Name, string Domain, (string Name, MatchMode Mode, MatchCondition[] Conditions)[] PageTypes)[] SeedSites =
    [
        ("Lastik Borsası", "lastikborsasi.com",
        [
            HomePage,
            ProductPage,
            ("Ebat", MatchMode.Any, [new(MatchConditionType.UrlRegex, @"^/\d{3}-\d{2}-r\d{2}-[a-z0-9-]+" + Query)]),
            ("Kategori", MatchMode.Any, [new(MatchConditionType.UrlRegex, @"^/[a-z0-9-]+-c-\d+" + Query)]),
            ("Marka kategorisi", MatchMode.Any, [new(MatchConditionType.UrlRegex, @"^/markalar/[a-z0-9-]+/[a-z0-9-]+-c-\d+" + Query)]),
            ("Lastik modeli", MatchMode.Any, [new(MatchConditionType.UrlRegex, @"^/(?![a-z0-9-]+-c-\d+(\?|$))[a-z][a-z0-9]*(-[a-z0-9]+)+" + Query)]),
            ("Hesap ve sepet", MatchMode.Any,
                [new(MatchConditionType.UrlGlob, "/cart"), new(MatchConditionType.UrlGlob, "/login"), new(MatchConditionType.UrlGlob, "/account/**")])
        ]),
        ("Tyre Supply", "tyresupply.com", [HomePage, ProductPage])
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var organization = await db.Organizations.OrderBy(o => o.Id).FirstOrDefaultAsync(ct);
        if (organization is null)
        {
            organization = Organization.Create("Varsayılan Organizasyon");
            db.Organizations.Add(organization);
        }

        foreach (var (name, domain, pageTypes) in SeedSites)
        {
            // Sayfa tipleri yalnızca site ilk oluşturulurken eklenir; sonradan silinen ya da yeniden adlandırılan tip geri gelmez.
            if (await db.Sites.AnyAsync(s => s.OrganizationId == organization.Id && s.Domain == domain, ct)) continue;

            var site = Site.Create(organization.Id, name, domain, clock.GetUtcNow());
            foreach (var (typeName, mode, conditions) in pageTypes) site.AddPageType(typeName, mode, conditions);
            db.Sites.Add(site);
        }

        await db.SaveChangesAsync(ct);
    }
}
