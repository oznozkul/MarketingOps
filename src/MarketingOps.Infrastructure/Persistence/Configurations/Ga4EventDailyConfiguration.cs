using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class Ga4EventDailyConfiguration : IEntityTypeConfiguration<Ga4EventDaily>
{
    public void Configure(EntityTypeBuilder<Ga4EventDaily> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("ga4_event_daily");
        builder.Property(x => x.EventName).HasMaxLength(100);
        builder.Property(x => x.EventValue).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.SiteId, x.Date, x.EventName }).IsUnique();
    }
}
