namespace MarketingOps.Domain.Common;

/// <summary>Kimliğe sahip domain nesnelerinin tabanı. İki entity, Id'leri aynıysa eşittir.</summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected init; } = Guid.CreateVersion7();

    /// <summary>Henüz yayınlanmamış olaylar. Altyapı kayıttan sonra yayınlar ve temizler.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}
