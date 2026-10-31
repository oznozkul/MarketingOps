namespace MarketingOps.Application.Crawls.Commands.RenderCrawl;

/// <summary>Taramada doğrudan 2xx dönen HTML sayfa; render örneği olmaya aday.</summary>
public sealed record RenderCandidate(string Url, Guid? PageTypeId);
