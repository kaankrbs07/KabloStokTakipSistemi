// Configuration/SmtpOptions.cs

namespace KabloStokTakipSistemi.Configuration
{
    /// SMTP konfigürasyonu (appsettings.json -> "Smtp")
    public sealed class SmtpOptions
    {
        public string Host { get; init; } = default!;
        public int Port { get; init; } = 465;
        public string SenderName { get; init; } = default!;
        public string SenderEmail { get; init; } = default!;
        public string Username { get; init; } = default!;
        public string Password { get; init; } = default!;
        public bool UseStartTls { get; init; } = false; // 587 için true
        public bool UseSsl { get; init; } = true; // 465 için true
        public bool UseOAuth2 { get; init; } = false;
    }
}