using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Tasks.Tracking;

namespace MarketingOps.IntegrationTests.Tasks;

/// <summary>Jira yerine: istekleri kaydeder, yeni kayıtlara sıralı anahtar verir.</summary>
public sealed class FakeTaskTracker : ITaskTracker
{
    public List<(TaskTrackerCredentials Credentials, ExternalIssueRequest Request)> Calls { get; } = [];

    public Task<ExternalIssue> UpsertAsync(TaskTrackerCredentials credentials, ExternalIssueRequest request, CancellationToken cancellationToken)
    {
        lock (Calls) Calls.Add((credentials, request));
        var key = request.ExistingKey ?? $"{credentials.ProjectKey}-{Calls.Count(c => c.Request.ExistingKey is null)}";
        return Task.FromResult(new ExternalIssue(key, $"{credentials.BaseUrl}/browse/{key}"));
    }
}
