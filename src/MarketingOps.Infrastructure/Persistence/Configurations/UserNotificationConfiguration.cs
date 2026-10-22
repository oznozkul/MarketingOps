using MarketingOps.Domain.Notifications;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.EventType).HasConversion<string>().HasMaxLength(40);
        builder.Property(n => n.Title).HasMaxLength(UserNotification.TitleMaxLength);
        builder.Property(n => n.Body).HasMaxLength(UserNotification.BodyMaxLength);
        builder.Property(n => n.Href).HasMaxLength(UserNotification.HrefMaxLength);
        builder.HasIndex(n => new { n.SiteId, n.ReadAt });
        builder.HasOne<Site>().WithMany().HasForeignKey(n => n.SiteId).OnDelete(DeleteBehavior.Cascade);
    }
}
