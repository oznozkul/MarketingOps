using MediatR;

namespace MarketingOps.Application.Tasks.Commands.RemoveTaskTrackerConnection;

/// <summary>Bağlantıyı ve şifreli anahtarı siler. Görevlerdeki Jira anahtarları bağlantı olarak kalır.</summary>
public sealed record RemoveTaskTrackerConnectionCommand(Guid SiteId) : IRequest;
