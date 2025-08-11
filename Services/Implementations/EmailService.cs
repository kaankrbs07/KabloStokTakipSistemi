using KabloStokTakipSistemi.Configuration;
using KabloStokTakipSistemi.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class EmailService : IEmailService
{
    private readonly SmtpOptions _opt;
    private readonly ILogger<EmailService> _log;

    public EmailService(IOptions<SmtpOptions> options, ILogger<EmailService> log)
    {
        _opt = options.Value;
        _log = log;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        string? textBody = null,
        IEnumerable<string>? cc = null,
        IEnumerable<string>? bcc = null,
        IEnumerable<(string FileName, byte[] Content)>? attachments = null,
        CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_opt.SenderName, _opt.SenderEmail));
        message.To.Add(MailboxAddress.Parse(to));

        if (cc != null)
            foreach (var c in cc)
                message.Cc.Add(MailboxAddress.Parse(c));
        if (bcc != null)
            foreach (var b in bcc)
                message.Bcc.Add(MailboxAddress.Parse(b));

        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody ?? HtmlToText(htmlBody)
        };

        if (attachments != null)
        {
            foreach (var (fileName, content) in attachments)
            {
                builder.Attachments.Add(fileName, content);
            }
        }

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            // Sertifika doğrulaması özel ortamlarda sorun çıkarırsa kontrollü esnetme:
            // client.ServerCertificateValidationCallback = (s, c, h, e) => true; // PROD'da önerilmez!

            var secure = _opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

            await client.ConnectAsync(_opt.Host, _opt.Port, secure, ct);

            if (!_opt.UseOAuth2)
                client.AuthenticationMechanisms.Remove("XOAUTH2");

            await client.AuthenticateAsync(_opt.Username, _opt.Password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _log.LogInformation("E-posta gönderildi: {Subject} -> {To}", subject, to);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "E-posta gönderilemedi: {Subject} -> {To}", subject, to);
            throw; // İstersen özel bir uygulama hatasına wrap edebilirsin.
        }
    }

    // Basit html
    private static string HtmlToText(string html) =>
        string.IsNullOrWhiteSpace(html)
            ? string.Empty
            : System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty).Trim();
}