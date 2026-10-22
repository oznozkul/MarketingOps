using Microsoft.AspNetCore.DataProtection;

namespace MarketingOps.Application.Tasks.Tracking;

/// <summary>Jira API anahtarı veritabanına ASP.NET Core Data Protection ile şifrelenerek yazılır.</summary>
public sealed class TaskTrackerTokenProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("MarketingOps.TaskTracker.ApiToken.v1");

    public string Protect(string token) => _protector.Protect(token);

    public string Unprotect(string protectedToken) => _protector.Unprotect(protectedToken);
}
