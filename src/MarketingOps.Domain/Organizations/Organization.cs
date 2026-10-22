using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Organizations;

public class Organization : Entity
{
    public const int NameMaxLength = 200;

    public string Name { get; private set; } = null!;

    private Organization() { }

    public static Organization Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Organizasyon adı boş olamaz.");

        return new Organization { Name = name.Trim() };
    }
}
