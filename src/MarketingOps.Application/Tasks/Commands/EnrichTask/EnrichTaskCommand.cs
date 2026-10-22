using MediatR;

namespace MarketingOps.Application.Tasks.Commands.EnrichTask;

/// <summary>İşçi tarafından çalıştırılır: AI ayarlıysa görevin etki ve öneri metnini zenginleştirir, değilse hiçbir şey yapmaz.</summary>
public sealed record EnrichTaskCommand(Guid TaskId) : IRequest;
