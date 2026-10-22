using MarketingOps.Domain.Crawls;

namespace MarketingOps.Infrastructure.Crawling;

public sealed record ParsedPage(PageSnapshotData Data, IReadOnlyList<Uri> InternalLinks);
