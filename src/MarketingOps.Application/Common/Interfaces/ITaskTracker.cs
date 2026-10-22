using MarketingOps.Application.Tasks.Tracking;

namespace MarketingOps.Application.Common.Interfaces;

/// <summary>Dış görev sistemi (Jira Cloud). Testte sahte uygulama kullanılır.</summary>
public interface ITaskTracker
{
    /// <summary><see cref="ExternalIssueRequest.ExistingKey"/> doluysa mevcut kaydı günceller, yoksa yenisini açar.</summary>
    Task<ExternalIssue> UpsertAsync(TaskTrackerCredentials credentials, ExternalIssueRequest request, CancellationToken cancellationToken);
}
