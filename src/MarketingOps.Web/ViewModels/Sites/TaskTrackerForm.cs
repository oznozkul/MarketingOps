using MarketingOps.Application.Tasks.Queries.GetTaskTrackerConnection;

namespace MarketingOps.Web.ViewModels.Sites;

/// <summary>Jira bağlantı formu. Kayıtlı bağlantıda API anahtarı boş bırakılırsa mevcut anahtar korunur.</summary>
public sealed class TaskTrackerForm
{
    public string BaseUrl { get; set; } = "";
    public string ProjectKey { get; set; } = "";
    public string IssueType { get; set; } = "Task";
    public string Email { get; set; } = "";
    public string? ApiToken { get; set; }
    public bool HasToken { get; set; }

    public static TaskTrackerForm From(TaskTrackerConnectionResponse? connection) => connection is null
        ? new TaskTrackerForm()
        : new TaskTrackerForm
        {
            BaseUrl = connection.BaseUrl, ProjectKey = connection.ProjectKey, IssueType = connection.IssueType, Email = connection.Email, HasToken = true
        };
}
