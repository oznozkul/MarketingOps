using MediatR;

namespace MarketingOps.Application.Rules.Commands.SetRuleEnabled;

/// <summary>Listedeki aç/kapa anahtarı: diğer ayarlara dokunmadan kuralı açar ya da kapatır. Ayar kaydının kimliğini döner.</summary>
public sealed record SetRuleEnabledCommand(Guid SiteId, string RuleCode, bool IsEnabled) : IRequest<Guid>;
