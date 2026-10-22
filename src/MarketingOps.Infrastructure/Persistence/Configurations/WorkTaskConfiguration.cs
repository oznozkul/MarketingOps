using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Sites;
using MarketingOps.Domain.WorkTasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Key).HasMaxLength(30);
        builder.Property(t => t.Title).HasMaxLength(500);
        builder.Property(t => t.AssignedTeam).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(t => t.ExternalTrackerKey).HasMaxLength(50);
        builder.Property(t => t.ExternalUrl).HasMaxLength(500);

        // Bulgu başına tek görev; anahtar numarası site içinde benzersiz.
        builder.HasIndex(t => t.FindingId).IsUnique();
        builder.HasIndex(t => new { t.SiteId, t.Number }).IsUnique();
        builder.HasIndex(t => new { t.SiteId, t.Status });
        builder.HasOne<Site>().WithMany().HasForeignKey(t => t.SiteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Finding>().WithMany().HasForeignKey(t => t.FindingId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.Suggestion).HasJsonValue();
        builder.Property<List<string>>("_references").HasColumnName("references");
        builder.Ignore(t => t.References);
        builder.Property<List<AcceptanceCriterion>>("_criteria").HasColumnName("criteria").HasJsonList();
        builder.Ignore(t => t.Criteria);
        builder.Property<List<TaskHistoryEntry>>("_history").HasColumnName("history").HasJsonList();
        builder.Ignore(t => t.History);
        builder.Property<List<TaskComment>>("_comments").HasColumnName("comments").HasJsonList();
        builder.Ignore(t => t.Comments);
        builder.Property<List<SuggestionFeedback>>("_feedback").HasColumnName("feedback").HasJsonList();
        builder.Ignore(t => t.Feedback);
    }
}
