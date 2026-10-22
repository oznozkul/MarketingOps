using MediatR;

namespace MarketingOps.Application.Sites.Commands.UpdateOrderSource;

/// <summary>Bağlantı cümlesi boşsa mevcut şifreli değer korunur. Sorgu boşsa kaynak silinir.</summary>
public sealed record UpdateOrderSourceCommand(Guid SiteId, string? ConnectionString, string? Query) : IRequest;
