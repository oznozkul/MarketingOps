using MarketingOps.Domain.Notifications;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class ReportScheduleConfiguration : IEntityTypeConfiguration<ReportSchedule>
{
    public void Configure(EntityTypeBuilder<ReportSchedule> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Period).HasConversion<string>().HasMaxLength(20);
        builder.Property<List<string>>("_recipients").HasColumnName("recipients").HasJsonList();
        builder.Ignore(s => s.Recipients);
        builder.HasIndex(s => new { s.SiteId, s.Period }).IsUnique();
        builder.HasOne<Site>().WithMany().HasForeignKey(s => s.SiteId).OnDelete(DeleteBehavior.Cascade);
    }
}
