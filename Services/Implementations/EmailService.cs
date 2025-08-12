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
    /// MailKit tabanlı e-posta gönderim servisi.
    /// SMTP ayarlarını appsettings.json -> "Smtp" bölümünden alır.
    /// </summary>
    public sealed class EmailService : IEmailService
    {
        private readonly SmtpOptions _opt;
        private readonly ILogger<EmailService> _log;

        public EmailService(IOptions<SmtpOptions> options, ILogger<EmailService> log)
        {
            _opt = options.Value ?? throw new ArgumentNullException(nameof(options));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// E-posta gönderir (HTML + düz metin, cc/bcc ve ekler destekli).
        /// </summary>
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
            // ---------- 1) Mesajı oluştur ----------
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("Alıcı e-posta adresi boş olamaz.", nameof(to));

            var message = new MimeMessage();

            // Gönderen
            message.From.Add(new MailboxAddress(_opt.SenderName ?? string.Empty, _opt.SenderEmail ?? string.Empty));

            // Alıcılar
            if (!TryAddMailbox(message.To, to))
                throw new ArgumentException($"Geçersiz alıcı e-posta adresi: {to}", nameof(to));

            if (cc is not null)
                foreach (var c in cc) TryAddMailbox(message.Cc, c);

            if (bcc is not null)
                foreach (var b in bcc) TryAddMailbox(message.Bcc, b);

            message.Subject = subject ?? string.Empty;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody ?? string.Empty,
                TextBody = string.IsNullOrWhiteSpace(textBody)
                    ? HtmlToText(htmlBody ?? string.Empty)
                    : textBody
            };

            if (attachments is not null)
            {
                foreach (var (fileName, content) in attachments)
                {
                    if (!string.IsNullOrWhiteSpace(fileName) && content is { Length: > 0 })
                        builder.Attachments.Add(fileName, content);
                }
            }

            message.Body = builder.ToMessageBody();

            // ---------- 2) SMTP bağlantısı ----------
            using var client = new SmtpClient
            {
                Timeout = 10000 // ms -> bekleme kilitlenmesin
            };

            // Test/geliştirme ortamında sertifika doğrulamasını kapat (prod'da kapat!)
            if (_opt.IgnoreCertificateErrors)
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            }

            try
            {
                // Secure mode seçimi
                var secure = ResolveSecureOption(_opt.UseSsl, _opt.UseStartTls, _opt.Port);

                await client.ConnectAsync(_opt.Host, _opt.Port, secure, ct);

                // XOAUTH2'yi kapat (parola ile giriş yapacaksak)
                if (!_opt.UseOAuth2)
                    client.AuthenticationMechanisms.Remove("XOAUTH2");

                // Kimlik doğrulama
                if (!string.IsNullOrWhiteSpace(_opt.Username))
                {
                    await client.AuthenticateAsync(_opt.Username, _opt.Password, ct);
                }

                // Gönder
                await client.SendAsync(message, ct);

                // Kapat
                await client.DisconnectAsync(true, ct);

                _log.LogInformation("E-posta gönderildi. To={To}; Subject={Subject}", to, subject);
            }
            catch (OperationCanceledException)
            {
                _log.LogWarning("E-posta gönderimi iptal edildi. To={To}; Subject={Subject}", to, subject);
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "E-posta gönderilemedi. To={To}; Subject={Subject}", to, subject);
                throw;
            }
        }

        // --------- Helpers ---------

        private static SecureSocketOptions ResolveSecureOption(bool useSsl, bool useStartTls, int port)
        {
            // Tipik kullanım:
            // - 465  -> SSL/TLS (implicit)
            // - 587  -> STARTTLS
            if (useSsl) return SecureSocketOptions.SslOnConnect;
            if (useStartTls) return SecureSocketOptions.StartTls;

            // Port'a göre makul varsayılan
            if (port == 465) return SecureSocketOptions.SslOnConnect;
            if (port == 587) return SecureSocketOptions.StartTls;

            return SecureSocketOptions.Auto;
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


