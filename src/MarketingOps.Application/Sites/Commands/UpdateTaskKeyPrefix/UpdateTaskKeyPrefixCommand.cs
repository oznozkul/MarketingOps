using MediatR;

namespace MarketingOps.Application.Sites.Commands.UpdateTaskKeyPrefix;

/// <summary>Yeni görevlerin anahtar öneki. Mevcut görevlerin anahtarı değişmez.</summary>
public sealed record UpdateTaskKeyPrefixCommand(Guid SiteId, string Prefix) : IRequest;
