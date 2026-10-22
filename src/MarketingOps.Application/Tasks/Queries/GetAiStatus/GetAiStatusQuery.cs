using MediatR;

namespace MarketingOps.Application.Tasks.Queries.GetAiStatus;

/// <summary>AI önerilerinin durumu; ayar ekranında gösterilir. Sağlayıcı ayarlı değilse null.</summary>
public sealed record GetAiStatusQuery : IRequest<AiStatusResponse?>;
