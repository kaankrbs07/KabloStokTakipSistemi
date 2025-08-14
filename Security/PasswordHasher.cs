using System.Security.Cryptography;

namespace KabloStokTakipSistemi.Security;

/// <summary>
/// PBKDF2 (SHA-256) tabanlı tek-dosyada şifreleme helper'ı.
/// Format: PBKDF2$<iter>$<saltB64>$<hashB64>
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 120_000; // 80k–150k arası ayarlanabilir
    private const int SaltSize   = 16;
    private const int HashSize   = 32;

    public static string Hash(string password)
    {
        if (password is null) throw new ArgumentNullException(nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(stored))
            return false;

        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2")
            return false;

        var iterations = int.Parse(parts[1]);
        var salt       = Convert.FromBase64String(parts[2]);
        var expected   = Convert.FromBase64String(parts[3]);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var actual = pbkdf2.GetBytes(expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}