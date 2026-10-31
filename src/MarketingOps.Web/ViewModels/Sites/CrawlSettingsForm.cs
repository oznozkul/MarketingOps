using MarketingOps.Application.Sites.Commands.UpdateCrawlSettings;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Web.ViewModels.Sites;

/// <summary>Hariç tutulan path'ler formda satır başına bir desen olarak tek metin alanında düzenlenir.</summary>
public sealed class CrawlSettingsForm
{
    public CrawlSchedule Schedule { get; set; }
    public int MaxRequestsPerSecond { get; set; }
    public int MaxPages { get; set; }
    public string? ExcludedPathPatterns { get; set; }
    public bool RenderEnabled { get; set; }
    public int RenderSamplesPerPageType { get; set; }

    public static CrawlSettingsForm From(CrawlSettingsResponse settings) => new()
    {
        Schedule = settings.Schedule,
        MaxRequestsPerSecond = settings.MaxRequestsPerSecond,
        MaxPages = settings.MaxPages,
        ExcludedPathPatterns = string.Join("\n", settings.ExcludedPathPatterns),
        RenderEnabled = settings.RenderEnabled,
        RenderSamplesPerPageType = settings.RenderSamplesPerPageType
    };

    public UpdateCrawlSettingsCommand ToCommand(Guid siteId) => new(
        siteId,
        Schedule,
        MaxRequestsPerSecond,
        MaxPages,
        (ExcludedPathPatterns ?? "").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
        RenderEnabled,
        RenderSamplesPerPageType);
}
