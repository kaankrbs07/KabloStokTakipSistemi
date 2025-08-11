namespace KabloStokTakipSistemi.Configuration;

public sealed class SmtpOptions
{
    public string Host { get; init; } = default!;
    public int Port { get; init; } = 587;
    public string SenderName { get; init; } = default!;
    public string SenderEmail { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
    public bool UseStartTls { get; init; } = true;
    public bool UseOAuth2 { get; init; } = false;
}
