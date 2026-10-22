using Hangfire.Dashboard;

namespace MarketingOps.Web.Infrastructure;

/// <summary>
/// Hangfire dashboard'u yalnızca Development'ta açılır; konteynerde istek "yerel" sayılmadığı için varsayılan filtre
/// erişimi engelliyor. Kimlik doğrulama fazında yetkiye bağlanacak.
/// </summary>
public sealed class DevelopmentDashboardAuthorization : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
