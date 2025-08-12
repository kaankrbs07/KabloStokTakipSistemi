// Configuration/SmtpOptions.cs
namespace KabloStokTakipSistemi.Configuration
{
    /// <summary>
    /// SMTP konfigürasyonu (appsettings.json -> "Smtp")
    /// </summary>
    public sealed class SmtpOptions
    {
        /// <summary>SMTP sunucu adresi (örn: smtp.gmail.com)</summary>
        public string Host { get; init; } = string.Empty;

        /// <summary>SMTP portu (465 = SSL, 587 = STARTTLS)</summary>
        public int Port { get; init; } = 465;

        /// <summary>Gönderici görünen adı</summary>
        public string SenderName { get; init; } = string.Empty;

        /// <summary>Gönderici e‑posta adresi</summary>
        public string SenderEmail { get; init; } = string.Empty;

        /// <summary>SMTP kullanıcı adı (genelde SenderEmail ile aynı)</summary>
        public string Username { get; init; } = string.Empty;

        /// <summary>SMTP şifresi / uygulama şifresi</summary>
        public string Password { get; init; } = string.Empty;

        /// <summary>587 gibi portlarda STARTTLS kullanılacaksa true.</summary>
        public bool UseStartTls { get; init; } = false;

        /// <summary>465 gibi portlarda SSL/TLS (implicit) kullanılacaksa true.</summary>
        public bool UseSsl { get; init; } = true;

        /// <summary>OAuth2 kullan (Gmail XOAUTH2). Parola ile girişte false kalsın.</summary>
        public bool UseOAuth2 { get; init; } = false;

        /// <summary>
        /// Yalnızca GELİŞTİRME/TEST için: Sertifika doğrulamasını yok say.
        /// Üretimde kesinlikle false bırak.
        /// </summary>
        public bool IgnoreCertificateErrors { get; init; } = false;
    }
}