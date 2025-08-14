using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KabloStokTakipSistemi.Configuration;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Middlewares;
using KabloStokTakipSistemi.Security;
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

    public AuthService(AppDbContext db, IOptions<JwtOptions> jwt, IUserService users /*ILogger<AuthService> _unused*/)
    {
        _db = db;
        _jwt = jwt.Value;
        _users = users;
    }

    // ---- LOGIN (Admin) ----
    public async Task<TokenResponse?> LoginAdminAsync(LoginAdminRequest req, CancellationToken ct = default)
    {
        // Admin tablosundan username ile çek, User join'i üzerinden güvenlik alanlarını al
        var row = await _db.Admins
            .Include(a => a.User)
            .AsNoTracking()
            .Where(a => a.Username == req.Username)
            .Select(a => new {
                a.UserID,
                Password = a.User!.Password,
                IsActive = a.User!.IsActive,
                Role     = a.User!.Role ?? "Admin",
                FullName = ((a.User!.FirstName ?? "") + " " + (a.User!.LastName ?? "")).Trim()
            })
            .FirstOrDefaultAsync(ct);

        if (row is null || !row.IsActive) return null;
        if (!PasswordHasher.Verify(req.Password, row.Password)) return null;

        return CreateToken(row.UserID, row.Role, row.FullName);
    }

    // ---- LOGIN (Employee) ----
    public async Task<TokenResponse?> LoginEmployeeAsync(LoginEmployeeRequest req, CancellationToken ct = default)
    {
        var row = await _db.Employees
            .Include(e => e.User)
            .AsNoTracking()
            .Where(e => e.EmployeeID == req.EmployeeID)
            .Select(e => new {
                e.UserID,
                Password = e.User!.Password,
                IsActive = e.User!.IsActive,
                Role     = e.User!.Role ?? "Employee",
                FullName = ((e.User!.FirstName ?? "") + " " + (e.User!.LastName ?? "")).Trim()
            })
            .FirstOrDefaultAsync(ct);

        if (row is null || !row.IsActive) return null;
        if (!PasswordHasher.Verify(req.Password, row.Password)) return null;

        return CreateToken(row.UserID, row.Role, row.FullName);
    }

    // ---- REGISTER (Employee) ----
    public async Task<bool> RegisterEmployeeAsync(RegisterEmployeeRequest req, CancellationToken ct = default)
    {
        // Basit parola politikası: ≥8, en az bir küçük ve bir büyük harf
        if (!PasswordPolicy.IsValid(req.Password))
            throw new AppException(AppErrors.Validation.BadRequest, "Şifre en az 8 karakter olmalı ve büyük/küçük harf içermelidir.");

        var hashed = PasswordHasher.Hash(req.Password);

        var dto = new CreateUserDto(
            req.UserID,
            req.FirstName, req.LastName,
            req.Email, req.PhoneNumber,
            req.DepartmentID,
            Role: "Employee", //Default olarak Employee
            IsActive: true,
            Password: hashed,
            AdminUsername: null,
            AdminDepartmentName: null
        );

        // sp_CreateUser üzerinden kullanıcı + (role=Employee ise) Employees insert’i yapılır
        return await _users.CreateUserAsync(dto);
    }

    // ---- JWT ----
    private TokenResponse CreateToken(long userId, string role, string? fullName)
    {
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()), // SESSION_CONTEXT için şart
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
