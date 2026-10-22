namespace MarketingOps.Web.Controllers.Api;

/// <summary>Site kimliği route'tan gelir; gövdede yalnızca değişen alan bulunur.</summary>
public sealed record UpdateSiteRequest(string Name);
