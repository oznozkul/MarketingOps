using System.Text.Json;
using MarketingOps.Domain.Crawls;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

/// <summary>
/// Snapshot verisi jsonb kolonuna JSON olarak yazılır. Gözlem kaydıdır ve sonradan değişmez; bu yüzden içinde
/// sorgulama yerine kayıt bütün olarak okunur (kural motoru da sayfaları bellekte değerlendirir).
/// </summary>
public sealed class PageSnapshotConfiguration : IEntityTypeConfiguration<PageSnapshot>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<PageSnapshot> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Url).HasMaxLength(PageSnapshot.UrlMaxLength);
        builder.Property(p => p.NormalizedUrl).HasMaxLength(PageSnapshot.UrlMaxLength);
        builder.Property(p => p.ContentType).HasMaxLength(200);
        builder.Property(p => p.ContentEncoding).HasMaxLength(50);
        builder.Property(p => p.CacheControl).HasMaxLength(500);
        builder.Property(p => p.FetchError).HasMaxLength(1000);
        builder.HasIndex(p => new { p.CrawlId, p.PageTypeId });
        builder.HasIndex(p => new { p.CrawlId, p.NormalizedUrl }).IsUnique();
        builder.HasOne<Crawl>().WithMany().HasForeignKey(p => p.CrawlId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.RedirectChain)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<List<RedirectHop>>(v, Json) ?? new List<RedirectHop>(),
                ImmutableComparer<IReadOnlyList<RedirectHop>>());

        builder.Property(p => p.Data)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<PageSnapshotData>(v, Json) ?? PageSnapshotData.Empty,
                ImmutableComparer<PageSnapshotData>());
    }

    // Snapshot değişmez; değişiklik takibi referans eşitliğiyle yeterli.
    private static ValueComparer<T> ImmutableComparer<T>() where T : class =>
        new((a, b) => ReferenceEquals(a, b), v => v.GetHashCode(), v => v);
}
