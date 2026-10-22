using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class SitemapStatusConfiguration : IEntityTypeConfiguration<SitemapStatus>
{
    public void Configure(EntityTypeBuilder<SitemapStatus> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("sitemap_statuses");
        builder.Property(x => x.Path).HasMaxLength(2048);
        builder.HasIndex(x => new { x.SiteId, x.Path }).IsUnique();
    }
}
