using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class TaskTrackerConnectionConfiguration : IEntityTypeConfiguration<TaskTrackerConnection>
{
    public void Configure(EntityTypeBuilder<TaskTrackerConnection> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.BaseUrl).HasMaxLength(500);
        builder.Property(c => c.ProjectKey).HasMaxLength(20);
        builder.Property(c => c.IssueType).HasMaxLength(100);
        builder.Property(c => c.Email).HasMaxLength(320);
        builder.HasIndex(c => c.SiteId).IsUnique();
        builder.HasOne<Site>().WithMany().HasForeignKey(c => c.SiteId).OnDelete(DeleteBehavior.Cascade);
    }
}
