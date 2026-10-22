using MarketingOps.Domain.Tracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class TagAuditResultConfiguration : IEntityTypeConfiguration<TagAuditResult>
{
    public void Configure(EntityTypeBuilder<TagAuditResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.PageLabel).HasMaxLength(TagAuditResult.LabelMaxLength);
        builder.Property(r => r.Url).HasMaxLength(2048);
        builder.Property(r => r.Vendor).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property<List<string>>("_foundIds").HasColumnName("found_ids").HasJsonList();
        builder.Ignore(r => r.FoundIds);
        builder.Property<List<CapturedTagRequest>>("_requests").HasColumnName("requests").HasJsonList();
        builder.Ignore(r => r.Requests);
        builder.HasIndex(r => new { r.AuditRunId, r.PageLabel, r.Vendor }).IsUnique();
    }
}
