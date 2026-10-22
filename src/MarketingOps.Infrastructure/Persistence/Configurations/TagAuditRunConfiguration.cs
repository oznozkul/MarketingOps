using MarketingOps.Domain.Sites;
using MarketingOps.Domain.Tracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class TagAuditRunConfiguration : IEntityTypeConfiguration<TagAuditRun>
{
    public void Configure(EntityTypeBuilder<TagAuditRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Error).HasMaxLength(TagAuditRun.ErrorMaxLength);
        builder.Property(r => r.OrderSourceError).HasMaxLength(TagAuditRun.ErrorMaxLength);
        builder.HasIndex(r => new { r.SiteId, r.Number }).IsUnique();
        builder.HasOne<Site>().WithMany().HasForeignKey(r => r.SiteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(r => r.Results).WithOne().HasForeignKey(x => x.AuditRunId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Results).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
