namespace MarketingOps.Application.Notifications;

public sealed class MailOptions
{
    public const string Section = "Mail";

    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public string User { get; init; } = "";
    public string Password { get; init; } = "";
    public string From { get; init; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From);
}
