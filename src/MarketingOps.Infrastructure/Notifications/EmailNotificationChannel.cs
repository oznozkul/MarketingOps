using MailKit.Net.Smtp;
using MailKit.Security;
using MarketingOps.Application.Notifications;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Notifications;
using MimeKit;

namespace MarketingOps.Infrastructure.Notifications;

public sealed class EmailNotificationChannel(MailOptions mail) : INotificationChannel
{
    public NotificationChannelKind Kind => NotificationChannelKind.Email;

    public async Task SendAsync(OutboundNotification message, string? secret, CancellationToken cancellationToken)
    {
        if (!mail.IsConfigured) throw new DomainException("SMTP yapılandırılmadı.");
        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(mail.From));
        foreach (var recipient in message.Recipients) mime.To.Add(MailboxAddress.Parse(recipient));
        mime.Subject = message.Title;
        mime.Body = message.Html
            ? new BodyBuilder { HtmlBody = message.Body }.ToMessageBody()
            : new BodyBuilder { TextBody = message.Href is null ? message.Body : message.Body + "\n" + message.Href }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(mail.Host, mail.Port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
        if (!string.IsNullOrWhiteSpace(mail.User))
            await client.AuthenticateAsync(mail.User, mail.Password, cancellationToken);
        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
