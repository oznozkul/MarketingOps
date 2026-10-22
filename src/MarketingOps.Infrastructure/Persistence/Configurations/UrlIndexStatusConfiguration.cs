using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class UrlIndexStatusConfiguration : IEntityTypeConfiguration<UrlIndexStatus>
{
    public void Configure(EntityTypeBuilder<UrlIndexStatus> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.Url).HasMaxLength(2048);
        builder.Property(x => x.Verdict).HasMaxLength(30);
        builder.Property(x => x.CoverageState).HasMaxLength(200);
        builder.Property(x => x.IndexingState).HasMaxLength(100);
        builder.Ignore(x => x.IsIndexed);
        builder.HasIndex(x => new { x.SiteId, x.Url }).IsUnique();
    }
}
