using MarketingOps.Domain.Notifications;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.EventType).HasConversion<string>().HasMaxLength(40);
        builder.Property(d => d.Channel).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(d => new { d.SiteId, d.EventType, d.FindingId, d.SentAt });
        builder.HasOne<Site>().WithMany().HasForeignKey(d => d.SiteId).OnDelete(DeleteBehavior.Cascade);
    }
}
