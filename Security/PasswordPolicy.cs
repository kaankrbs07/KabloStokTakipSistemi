using System.Text.RegularExpressions;

namespace KabloStokTakipSistemi.Security;

/// <summary>
/// Minimum 8 karakter, en az bir büyük (Lu) ve bir küçük (Ll) harf.
/// Unicode sınıfları Türkçe karakterleri kapsar (İ/ı/ş/ğ/ç/ö/ü).
/// </summary>
public static class PasswordPolicy
{
    private const string Pattern = @"^(?=.*\p{Ll})(?=.*\p{Lu}).{8,}$";

    public static bool IsValid(string password) =>
        !string.IsNullOrWhiteSpace(password) && Regex.IsMatch(password, Pattern);
}