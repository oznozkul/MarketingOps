using MarketingOps.Domain.Notifications;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class NotificationRuleConfiguration : IEntityTypeConfiguration<NotificationRule>
{
    public void Configure(EntityTypeBuilder<NotificationRule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.EventType).HasConversion<string>().HasMaxLength(40);
        builder.Property<List<NotificationChannelKind>>("_channels").HasColumnName("channels").HasJsonList();
        builder.Ignore(r => r.Channels);
        builder.Property<List<string>>("_recipients").HasColumnName("recipients").HasJsonList();
        builder.Ignore(r => r.Recipients);
        builder.Ignore(r => r.QuietHours);
        builder.HasIndex(r => new { r.SiteId, r.EventType }).IsUnique();
        builder.HasOne<Site>().WithMany().HasForeignKey(r => r.SiteId).OnDelete(DeleteBehavior.Cascade);
    }
}
