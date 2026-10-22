namespace MarketingOps.Web.Filters;

/// <summary>
/// Site değiştirildiğinde bu action'ın diğer siteye ait karşılığı yoksa (ör. bir bulgunun detayı)
/// aynı controller'daki hangi action'a gidileceğini belirtir.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SiteSwitchFallbackAttribute(string action) : Attribute
{
    public string Action { get; } = action;
}
