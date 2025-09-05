using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KabloStokTakipSistemi.Configuration;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Middlewares; // AppErrors, AppException, ErrorBody
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;
    private readonly IUserService _users;
    
    public AuthService(AppDbContext db, IOptions<JwtOptions> jwt, IUserService users)
    {
        _db = db;
        _jwt = jwt.Value;
        _users = users;

        if (string.IsNullOrWhiteSpace(_jwt.Key))
            throw new AppException(AppErrors.Common.Unexpected, "JWT Key yapılandırması eksik.");
    }

    // ---- LOGIN (Admin) ----
    public async Task<TokenResponse?> LoginAdminAsync(LoginAdminRequest req, CancellationToken ct = default)
    {
        var row = await _db.Admins
            .Include(a => a.User)
            .AsNoTracking()
            .Where(a => a.Username == req.Username)
            .Select(a => new
            {
                a.UserID,
                Password = a.User!.Password,
                IsActive = a.User!.IsActive,
                Role     = a.User!.Role ?? "Admin",
                FullName = ((a.User!.FirstName ?? "") + " " + (a.User!.LastName ?? "")).Trim()
            })
            .FirstOrDefaultAsync(ct);
        
        if (row is null || !row.IsActive)
            throw new AppException(AppErrors.Common.Unauthorized, "Kimlik doğrulama başarısız.");


        if (!string.Equals(req.Password, row.Password, StringComparison.Ordinal))
            throw new AppException(AppErrors.Common.Unauthorized, "Kimlik doğrulama başarısız.");

        return CreateToken(row.UserID, row.Role, row.FullName);
    }

    // ---- LOGIN (Employee) ----
    public async Task<TokenResponse?> LoginEmployeeAsync(LoginEmployeeRequest req, CancellationToken ct = default)
    {
        var row = await _db.Employees
            .Include(e => e.User)
            .AsNoTracking()
            .Where(e => e.EmployeeID == req.EmployeeID)
            .Select(e => new
            {
                e.UserID,
                Password = e.User!.Password,
                IsActive = e.User!.IsActive,
                Role     = e.User!.Role ?? "Employee",
                FullName = ((e.User!.FirstName ?? "") + " " + (e.User!.LastName ?? "")).Trim()
            })
            .FirstOrDefaultAsync(ct);

        if (row is null || !row.IsActive)
            throw new AppException(AppErrors.Common.Unauthorized, "Kimlik doğrulama başarısız.");

        // NOT: Parola doğrulama (hash) katmanınız hazırsa burada kullanın.
        if (!string.Equals(req.Password, row.Password, StringComparison.Ordinal))
            throw new AppException(AppErrors.Common.Unauthorized, "Kimlik doğrulama başarısız.");

        return CreateToken(row.UserID, row.Role, row.FullName);
    }

    // ---- JWT ----
    private TokenResponse CreateToken(long userId, string role, string? fullName)
    {
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, fullName ?? string.Empty)
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var exp   = now.AddMinutes(_jwt.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: exp,
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenResponse(jwt, exp, userId, role, fullName);
    }
}

