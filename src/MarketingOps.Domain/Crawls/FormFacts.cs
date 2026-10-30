namespace MarketingOps.Domain.Crawls;

/// <summary>Kullanıcının doldurduğu form alanları ve etiketi olmayanlar. Placeholder etiket sayılmaz.</summary>
public sealed record FormFacts
{
    public int Fields { get; init; }
    public ElementIssue Unlabeled { get; init; } = ElementIssue.None;

    public static FormFacts None { get; } = new();
}
