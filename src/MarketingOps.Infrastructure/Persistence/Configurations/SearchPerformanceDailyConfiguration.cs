using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class SearchPerformanceDailyConfiguration : IEntityTypeConfiguration<SearchPerformanceDaily>
{
    public void Configure(EntityTypeBuilder<SearchPerformanceDaily> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("search_performance_daily");
        builder.Property(x => x.Url).HasMaxLength(2048);
        builder.HasIndex(x => new { x.SiteId, x.Date, x.Url }).IsUnique();
    }
}
