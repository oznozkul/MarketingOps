using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

/// <summary>Render kanıt ekran görüntüsü; bytea olarak saklanır, taraması silinince cascade ile gider.</summary>
public sealed class RenderScreenshotConfiguration : IEntityTypeConfiguration<RenderScreenshot>
{
    public void Configure(EntityTypeBuilder<RenderScreenshot> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Url).HasMaxLength(RenderScreenshot.UrlMaxLength);
        builder.Property(r => r.Device).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Bytes).HasColumnType("bytea");
        builder.HasIndex(r => new { r.CrawlId, r.Url, r.Device });
        builder.HasOne<Crawl>().WithMany().HasForeignKey(r => r.CrawlId).OnDelete(DeleteBehavior.Cascade);
    }
}
