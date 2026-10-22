using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

public sealed class ConnectorAccountConfiguration : IEntityTypeConfiguration<ConnectorAccount>
{
    public void Configure(EntityTypeBuilder<ConnectorAccount> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.ExternalAccountId).HasMaxLength(500);
        builder.Property(a => a.ExternalAccountName).HasMaxLength(500);
        builder.Property(a => a.AccountEmail).HasMaxLength(320);
        builder.Property(a => a.LastError).HasMaxLength(ConnectorAccount.ErrorMaxLength);
        builder.Property<List<string>>("_scopes").HasColumnName("scopes");
        builder.Ignore(a => a.Scopes);
        builder.Ignore(a => a.HasSecret);
        builder.HasIndex(a => new { a.SiteId, a.Type }).IsUnique();
        builder.HasOne<Site>().WithMany().HasForeignKey(a => a.SiteId).OnDelete(DeleteBehavior.Cascade);
    }
}
