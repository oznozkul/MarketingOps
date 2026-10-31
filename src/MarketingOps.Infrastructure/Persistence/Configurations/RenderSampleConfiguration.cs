using System.Text.Json;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

/// <summary>Ölçüm jsonb'ye bütün olarak yazılır; snapshot gibi gözlem kaydıdır, sonradan değişmez.</summary>
public sealed class RenderSampleConfiguration : IEntityTypeConfiguration<RenderSample>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<RenderSample> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Url).HasMaxLength(RenderSample.UrlMaxLength);
        builder.Property(r => r.Device).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Error).HasMaxLength(RenderSample.ErrorMaxLength);
        builder.Ignore(r => r.HasClearView);
        builder.Ignore(r => r.HasMeasurements);
        builder.HasIndex(r => new { r.CrawlId, r.PageTypeId });
        builder.HasIndex(r => new { r.CrawlId, r.Url, r.Device }).IsUnique();
        builder.HasOne<Crawl>().WithMany().HasForeignKey(r => r.CrawlId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.Measurements)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<RenderMeasurements>(v, Json) ?? RenderMeasurements.Empty,
                new ValueComparer<RenderMeasurements>((a, b) => ReferenceEquals(a, b), v => v.GetHashCode(), v => v));
    }
}
