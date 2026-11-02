using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Sites.Queries.GetSites;

public sealed record SiteListItemResponse(Guid Id, string Name, string Domain, bool IsOwnershipVerified, int PageTypeCount, SiteSetupStep? SetupStep);
