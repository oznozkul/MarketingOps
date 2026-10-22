using MediatR;

namespace MarketingOps.Application.Notifications.Commands.SaveNotificationChannels;

public sealed record SaveNotificationChannelsCommand(Guid SiteId, string? TeamsWebhook, string? SlackWebhook, bool RemoveTeams, bool RemoveSlack) : IRequest;
