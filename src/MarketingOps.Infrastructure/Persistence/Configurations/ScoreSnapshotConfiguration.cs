using MarketingOps.Domain.Scores;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class ScoreSnapshotConfiguration : IEntityTypeConfiguration<ScoreSnapshot>
{
    public void Configure(EntityTypeBuilder<ScoreSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Category).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(s => new { s.SiteId, s.Category, s.Date }).IsUnique();
        builder.HasOne<Site>().WithMany().HasForeignKey(s => s.SiteId).OnDelete(DeleteBehavior.Cascade);
    }
}
