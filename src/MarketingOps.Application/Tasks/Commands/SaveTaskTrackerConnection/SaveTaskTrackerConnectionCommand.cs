using MediatR;

namespace MarketingOps.Application.Tasks.Commands.SaveTaskTrackerConnection;

/// <summary>Jira bağlantısını kaydeder. Güncellemede <see cref="ApiToken"/> boş bırakılırsa kayıtlı anahtar korunur.</summary>
public sealed record SaveTaskTrackerConnectionCommand(Guid SiteId, string BaseUrl, string ProjectKey, string IssueType, string Email, string? ApiToken)
    : IRequest<Guid>;
