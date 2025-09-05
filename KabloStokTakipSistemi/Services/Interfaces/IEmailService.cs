namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IEmailService
{
    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        string? textBody = null,
        IEnumerable<string>? cc = null,
        IEnumerable<string>? bcc = null,
        IEnumerable<(string FileName, byte[] Content)>? attachments = null,
        CancellationToken ct = default);
}