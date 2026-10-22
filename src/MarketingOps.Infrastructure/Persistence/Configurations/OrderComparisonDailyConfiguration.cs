using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class OrderComparisonDailyConfiguration : IEntityTypeConfiguration<OrderComparisonDaily>
{
    public void Configure(EntityTypeBuilder<OrderComparisonDaily> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Ga4PurchaseCount).HasColumnName("ga4_purchase_count");
        builder.Property(x => x.Ga4PurchaseValue).HasColumnName("ga4_purchase_value").HasPrecision(18, 2);
        builder.Property(x => x.OrderRevenue).HasPrecision(18, 2);
        builder.Property(x => x.GapPercent).HasPrecision(9, 2);
        builder.HasIndex(x => new { x.SiteId, x.Date }).IsUnique();
        builder.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("order_comparison_daily");
    }
}
