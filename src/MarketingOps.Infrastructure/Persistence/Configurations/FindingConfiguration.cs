using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class FindingConfiguration : IEntityTypeConfiguration<Finding>
{
    public void Configure(EntityTypeBuilder<Finding> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.RuleCode).HasMaxLength(100);
        builder.Property(f => f.Fingerprint).HasMaxLength(64);
        builder.Property(f => f.Title).HasMaxLength(500);
        builder.Property(f => f.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.FalsePositiveReason).HasMaxLength(Finding.ReasonMaxLength);
        builder.Ignore(f => f.IsActive);

        // Aynı kural + aynı sayfa tipi tek bulgudur; eşzamanlı iki değerlendirme kopya açamaz.
        builder.HasIndex(f => new { f.SiteId, f.Fingerprint }).IsUnique();
        builder.HasIndex(f => new { f.SiteId, f.Status });
        builder.HasIndex(f => new { f.SiteId, f.Status, f.Category });
        builder.HasOne<Site>().WithMany().HasForeignKey(f => f.SiteId).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(f => f.Occurrences, occurrences => occurrences.ToJson());
        builder.Navigation(f => f.Occurrences).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(f => f.History, history =>
        {
            history.ToJson();
            history.Property(h => h.Type).HasConversion<string>();
        });
        builder.Navigation(f => f.History).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
