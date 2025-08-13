using System.Text.RegularExpressions;
using KabloStokTakipSistemi.Configuration;
using KabloStokTakipSistemi.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace KabloStokTakipSistemi.Services.Implementations
{
    /// <summary>
    /// MailKit tabanlı e-posta servisi.
    /// appsettings.json -> "Smtp" bölümünden ayarları alır.
    /// TLS modu port ve UseStartTls'a göre otomatik seçilir:
    ///   - 465 => SslOnConnect
    ///   - 587 + UseStartTls=true => StartTls
    ///   - Diğerleri => Auto
    /// </summary>
    public sealed class EmailService : IEmailService
    {
        private readonly SmtpOptions _opt;
        private readonly ILogger<EmailService> _log;

        // Basit retry ayarları
        private const int DefaultTimeoutMs = 10000; // SmtpClient.Timeout
        private const int MaxRetries = 2; // toplam deneme: 1 + MaxRetries
        private const int RetryDelayMs = 1500;

        public EmailService(IOptions<SmtpOptions> options, ILogger<EmailService> log)
        {
            _opt = options.Value ?? throw new ArgumentNullException(nameof(options));
            _log = log ?? throw new ArgumentNullException(nameof(log));
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
            if (string.IsNullOrWhiteSpace(_opt.Host))
                throw new InvalidOperationException("SMTP Host yapılandırılmamış.");
            if (_opt.Port <= 0)
                throw new InvalidOperationException("SMTP Port geçersiz.");
            if (string.IsNullOrWhiteSpace(_opt.SenderEmail))
                throw new InvalidOperationException("Gönderen e-posta (SenderEmail) yapılandırılmamış.");

            // MIME mesajını kur
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_opt.SenderName ?? string.Empty, _opt.SenderEmail));
            if (!TryAddMailbox(message.To, to))
                throw new ArgumentException($"Geçersiz alıcı e-posta adresi: {to}", nameof(to));

            if (cc != null)
                foreach (var c in cc)
                    TryAddMailbox(message.Cc, c);

            if (bcc != null)
                foreach (var b in bcc)
                    TryAddMailbox(message.Bcc, b);

            message.Subject = subject ?? string.Empty;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody ?? string.Empty,
                TextBody = string.IsNullOrWhiteSpace(textBody) ? HtmlToText(htmlBody ?? string.Empty) : textBody
            };

            if (attachments != null)
            {
                foreach (var (fileName, content) in attachments)
                {
                    if (!string.IsNullOrWhiteSpace(fileName) && content is { Length: > 0 })
                        builder.Attachments.Add(fileName, content);
                }
            }

            message.Body = builder.ToMessageBody();

            //  SMTP gönderimi (basit retry ile)
            var attempt = 0;
            Exception? lastError = null;

            while (attempt <= MaxRetries)
            {
                attempt++;
                using var client = new SmtpClient { Timeout = DefaultTimeoutMs };

                try
                {
                    var secure = ChooseSecureSocketOption(_opt.Port, _opt.UseStartTls);

                    await client.ConnectAsync(_opt.Host, _opt.Port, secure, ct);

                    if (!string.IsNullOrWhiteSpace(_opt.Username))
                        await client.AuthenticateAsync(_opt.Username, _opt.Password, ct);

                    await client.SendAsync(message, ct);
                    await client.DisconnectAsync(true, ct);

                    _log.LogInformation("E-posta gönderildi. To={To}; Subject={Subject}; Attempt={Attempt}",
                        to, subject, attempt);
                    return;
                }
                catch (OperationCanceledException)
                {
                    _log.LogWarning("E-posta gönderimi iptal edildi. To={To}; Subject={Subject}; Attempt={Attempt}",
                        to, subject, attempt);
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _log.LogWarning(ex,
                        "E-posta gönderim denemesi başarısız. To={To}; Subject={Subject}; Attempt={Attempt}",
                        to, subject, attempt);

                    if (attempt > MaxRetries)
                        break;

                    try
                    {
                        await Task.Delay(RetryDelayMs, ct);
                    }
                    catch
                    {
                        /* ignore */
                    }
                }
            }

            _log.LogError(lastError, "E-posta gönderilemedi. To={To}; Subject={Subject}", to, subject);
            throw lastError ?? new Exception("E-posta gönderimi başarısız.");
        }

        // ----------------- Helpers -----------------

        private static SecureSocketOptions ChooseSecureSocketOption(int port, bool useStartTls)
        {
            if (port == 465) return SecureSocketOptions.SslOnConnect; // Gmail/SSL
            if (port == 587 && useStartTls) return SecureSocketOptions.StartTls; // STARTTLS
            return SecureSocketOptions.Auto; // diğerleri
        }

        private static bool TryAddMailbox(InternetAddressList list, string? address)
        {
            if (string.IsNullOrWhiteSpace(address)) return false;
            try
            {
                list.Add(MailboxAddress.Parse(address));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string HtmlToText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            return Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }
    }
}