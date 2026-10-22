using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class PageTypeConfiguration : IEntityTypeConfiguration<PageType>
{
    public void Configure(EntityTypeBuilder<PageType> builder)
    {
        builder.ToTable("page_types");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Name).HasMaxLength(PageType.NameMaxLength).IsRequired();
        builder.Property(p => p.Mode).HasConversion<string>().HasMaxLength(10);

        // Koşullar sayfa tipinden ayrı sorgulanmaz; jsonb kolonunda saklanır.
        builder.OwnsMany(p => p.Conditions, conditions =>
        {
            conditions.ToJson();
            conditions.Property(c => c.Type).HasConversion<string>();
            conditions.Property(c => c.Value);
        });
        builder.Navigation(p => p.Conditions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
