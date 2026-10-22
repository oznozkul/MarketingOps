using MarketingOps.Infrastructure.Persistence;
using MarketingOps.Infrastructure.Persistence.Seed;
using MarketingOps.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.IntegrationTests.Persistence;

[Collection(IntegrationTestCollection.Name)]
public class DevelopmentSeederTests(IntegrationTestContext context)
{
    [Fact]
    public async Task Seeding_twice_does_not_duplicate()
    {
        // Uygulama başlarken bir kez seed edildi; tekrar çalıştırmak hiçbir şey eklememeli.
        await using var scope = context.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = (await db.Organizations.CountAsync(), await db.Sites.CountAsync(s => s.Domain == "lastikborsasi.com" || s.Domain == "tyresupply.com"));

        await scope.ServiceProvider.GetRequiredService<DevelopmentSeeder>().SeedAsync();

        var after = (await db.Organizations.CountAsync(), await db.Sites.CountAsync(s => s.Domain == "lastikborsasi.com" || s.Domain == "tyresupply.com"));
        after.ShouldBe(before);
        after.ShouldBe((1, 2));

        var pageTypes = await db.Sites.AsNoTracking().Where(s => s.Domain == "lastikborsasi.com").SelectMany(s => s.PageTypes).ToListAsync();
        pageTypes.Select(p => p.Name).ShouldBe(["Ana sayfa", "Ürün", "Ebat", "Kategori", "Marka kategorisi", "Lastik modeli", "Hesap ve sepet"], ignoreOrder: true);
    }

    [Theory]
    [InlineData("/", "Ana sayfa")]
    [InlineData("/185-65-r15-yaz-lastigi", "Ebat")]
    [InlineData("/215-65-r16-lastik-fiyatlari?sayfa=2", "Ebat")]
    [InlineData("/otomobil-lastikleri-c-271462", "Kategori")]
    [InlineData("/markalar/michelin/suv-4x4-lastikleri-c-93097", "Marka kategorisi")]
    [InlineData("/michelin-primacy-4-fiyatlari", "Lastik modeli")]
    [InlineData("/Cart", "Hesap ve sepet")]
    [InlineData("/account/register", "Hesap ve sepet")]
    public async Task Seeded_page_types_match_real_urls_exactly_once(string path, string expected)
    {
        await using var scope = context.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pageTypes = await db.Sites.AsNoTracking().Where(s => s.Domain == "lastikborsasi.com").SelectMany(s => s.PageTypes).ToListAsync();

        var url = new Uri("https://www.lastikborsasi.com" + path);
        pageTypes.Where(p => p.Matches(url, ["BreadcrumbList"])).Select(p => p.Name).ShouldBe([expected]);
    }

    [Fact]
    public async Task Reseeding_does_not_bring_back_a_removed_page_type()
    {
        await using var scope = context.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var site = await db.Sites.Include(s => s.PageTypes).SingleAsync(s => s.Domain == "tyresupply.com");
        var home = site.PageTypes.SingleOrDefault(p => p.Name == "Ana sayfa");
        if (home is not null) site.RemovePageType(home.Id);
        await db.SaveChangesAsync();

        await scope.ServiceProvider.GetRequiredService<DevelopmentSeeder>().SeedAsync();

        (await db.Sites.AsNoTracking().Where(s => s.Domain == "tyresupply.com").SelectMany(s => s.PageTypes).AnyAsync(p => p.Name == "Ana sayfa")).ShouldBeFalse();
    }
}
