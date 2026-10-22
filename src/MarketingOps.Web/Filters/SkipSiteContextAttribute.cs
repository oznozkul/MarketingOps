namespace MarketingOps.Web.Filters;

/// <summary>Site bağlamı gerektirmeyen action'lar (ör. hata sayfası).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipSiteContextAttribute : Attribute;
