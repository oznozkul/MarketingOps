using MarketingOps.Domain.Organizations;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Name).HasMaxLength(Site.NameMaxLength).IsRequired();
        builder.Property(s => s.Domain).HasMaxLength(Site.DomainMaxLength).IsRequired();
        builder.Property(s => s.TaskKeyPrefix).HasMaxLength(Site.TaskKeyPrefixMaxLength).HasDefaultValue("");
        builder.HasIndex(s => new { s.OrganizationId, s.Domain }).IsUnique();
        builder.HasOne<Organization>().WithMany().HasForeignKey(s => s.OrganizationId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(s => s.CrawlSettings, settings =>
        {
            settings.Property(c => c.Schedule).HasConversion<string>().HasMaxLength(20).HasColumnName("crawl_schedule");
            settings.Property(c => c.MaxRequestsPerSecond).HasColumnName("crawl_max_requests_per_second");
            settings.Property(c => c.MaxPages).HasColumnName("crawl_max_pages");
            settings.Property(c => c.ExcludedPathPatterns).HasColumnName("crawl_excluded_path_patterns");
            settings.Ignore(c => c.RespectRobotsTxt);
        });
        builder.Navigation(s => s.CrawlSettings).IsRequired();

        builder.OwnsOne(s => s.ExpectedTrackingIds, ids =>
        {
            ids.Property(i => i.Ga4MeasurementId).HasMaxLength(30).HasColumnName("expected_ga4_measurement_id");
            ids.Property(i => i.GtmContainerId).HasMaxLength(30).HasColumnName("expected_gtm_container_id");
            ids.Property(i => i.MetaPixelId).HasMaxLength(30).HasColumnName("expected_meta_pixel_id");
            ids.Property(i => i.GoogleAdsConversionId).HasMaxLength(30).HasColumnName("expected_google_ads_conversion_id");
        });
        builder.Navigation(s => s.ExpectedTrackingIds).IsRequired();

        builder.HasMany(s => s.PageTypes).WithOne().HasForeignKey(p => p.SiteId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.PageTypes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
