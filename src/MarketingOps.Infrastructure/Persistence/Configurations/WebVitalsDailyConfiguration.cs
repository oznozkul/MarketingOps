using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class WebVitalsDailyConfiguration : IEntityTypeConfiguration<WebVitalsDaily>
{
    public void Configure(EntityTypeBuilder<WebVitalsDaily> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("web_vitals_daily");
        builder.Property(x => x.Url).HasMaxLength(2048);
        builder.Property(x => x.Strategy).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.SiteId, x.Date, x.Url, x.Strategy }).IsUnique();
    }
}
