using MarketingOps.Domain.Notifications;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class NotificationChannelConfigConfiguration : IEntityTypeConfiguration<NotificationChannelConfig>
{
    public void Configure(EntityTypeBuilder<NotificationChannelConfig> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.ProtectedTeamsWebhook).HasMaxLength(2000);
        builder.Property(c => c.ProtectedSlackWebhook).HasMaxLength(2000);
        builder.Ignore(c => c.HasTeams);
        builder.Ignore(c => c.HasSlack);
        builder.HasIndex(c => c.SiteId).IsUnique();
        builder.HasOne<Site>().WithMany().HasForeignKey(c => c.SiteId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("notification_channel_config");
    }
}
