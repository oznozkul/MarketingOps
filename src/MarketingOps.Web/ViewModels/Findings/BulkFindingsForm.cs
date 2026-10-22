using MarketingOps.Application.Findings.Commands.BulkUpdateFindings;

namespace MarketingOps.Web.ViewModels.Findings;

/// <summary>Toplu işlem formu; seçili bulgu kimlikleri istemcide gizli alanlara yazılır.</summary>
public sealed class BulkFindingsForm
{
    public List<Guid> Ids { get; set; } = [];
    public BulkFindingAction Action { get; set; }
    public string? Reason { get; set; }
    public DateOnly? Until { get; set; }
}
