using KabloStokTakipSistemi.DTOs.Users;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IAuthService
{
    Task<TokenResponse?> LoginAdminAsync(LoginAdminRequest req, CancellationToken ct = default);
    Task<TokenResponse?> LoginEmployeeAsync(LoginEmployeeRequest req, CancellationToken ct = default);
}