using System.Text.Json;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

/// <summary>Sayfa tipi önemleri ve eşikler küçük sözlüklerdir; jsonb olarak bütün halinde saklanır.</summary>
public sealed class SiteRuleSettingConfiguration : IEntityTypeConfiguration<SiteRuleSetting>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

    public void Configure(EntityTypeBuilder<SiteRuleSetting> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.RuleCode).HasMaxLength(100);
        builder.Property(s => s.SeverityOverride).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(s => new { s.SiteId, s.RuleCode }).IsUnique();
        builder.HasOne<Site>().WithMany().HasForeignKey(s => s.SiteId).OnDelete(DeleteBehavior.Cascade);

        builder.Property<Dictionary<Guid, Severity>>("_pageTypeSeverities")
            .HasColumnName("page_type_severities")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<Dictionary<Guid, Severity>>(v, Json) ?? new Dictionary<Guid, Severity>(),
                DictionaryComparer<Guid, Severity>());
        builder.Ignore(s => s.PageTypeSeverities);

        builder.Property<Dictionary<string, decimal>>("_thresholds")
            .HasColumnName("thresholds")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, Json) ?? new Dictionary<string, decimal>(),
                DictionaryComparer<string, decimal>());
        builder.Ignore(s => s.Thresholds);
    }

    // Update sözlüğü yeni örnekle değiştirir; yine de içerik karşılaştırması güvenli olan yoldur.
    private static ValueComparer<Dictionary<TKey, TValue>> DictionaryComparer<TKey, TValue>() where TKey : notnull =>
        new((a, b) => a!.Count == b!.Count && !a.Except(b).Any(),
            v => v.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
            v => new Dictionary<TKey, TValue>(v));
}
