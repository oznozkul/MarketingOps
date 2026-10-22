using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class CrawlConfiguration : IEntityTypeConfiguration<Crawl>
{
    public void Configure(EntityTypeBuilder<Crawl> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.TriggeredBy).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(c => new { c.SiteId, c.Number }).IsUnique();

        // Site başına aynı anda tek aktif tarama: eşzamanlı iki "Şimdi tara" isteğinin ikincisini veritabanı reddeder.
        builder.HasIndex(c => c.SiteId)
            .IsUnique()
            .HasFilter("status IN ('Queued', 'Running')")
            .HasDatabaseName("ix_crawls_one_active_per_site");

        builder.HasOne<Site>().WithMany().HasForeignKey(c => c.SiteId).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(c => c.Stages, stages =>
        {
            stages.ToJson();
            stages.Property(s => s.Stage).HasConversion<string>();
        });
        builder.Navigation(c => c.Stages).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(c => c.SkippedUrls, skipped =>
        {
            skipped.ToJson();
            skipped.Property(s => s.Reason).HasConversion<string>();
        });
        builder.Navigation(c => c.SkippedUrls).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(c => c.AssetChecks, assets => assets.ToJson());
        builder.Navigation(c => c.AssetChecks).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
