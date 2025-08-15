using System.Data;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Middlewares; // AppException/AppErrors
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class AdminService : IAdminService
{
    private readonly AppDbContext _context;

    // ILogger kaldırıldı — merkezi NLog + middleware kullanılıyor
    public AdminService(AppDbContext context) => _context = context;

    public async Task<IEnumerable<GetAdminDto>> GetAllAdminsAsync()
    {
        var admins = await _context.Admins
            .Include(a => a.User)
            .AsNoTracking()
            .ToListAsync();

        return admins.Select(a => new GetAdminDto(
            a.AdminID,
            a.Username,
            a.DepartmentName,
            a.User?.FirstName,
            a.User?.LastName
        ));
    }

    public async Task<GetAdminDto?> GetAdminByIdAsync(long adminId)
    {
        var a = await _context.Admins
            .Include(x => x.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AdminID == adminId);

        return a is null
            ? null
            : new GetAdminDto(
                a.AdminID,
                a.Username,
                a.DepartmentName,
                a.User?.FirstName,
                a.User?.LastName
            );
    }

    public async Task<bool> CreateAdminAsync(CreateUserDto userDto, CreateAdminDto adminDto)
    {
        if (string.IsNullOrWhiteSpace(adminDto.Username))
            throw new AppException(AppErrors.Validation.BadRequest, "Admin username boş olamaz.");
        if (string.IsNullOrWhiteSpace(adminDto.DepartmentName))
            throw new AppException(AppErrors.Validation.BadRequest, "Admin department boş olamaz.");

        // Çakışmalar
        var userExists = await _context.Users.AsNoTracking()
            .AnyAsync(u => u.UserID == userDto.UserID);
        if (userExists)
            throw new AppException(AppErrors.Common.Conflict, "Bu UserID zaten mevcut.");

        var usernameExists = await _context.Admins.AsNoTracking()
            .AnyAsync(a => a.Username == adminDto.Username);
        if (usernameExists)
            throw new AppException(AppErrors.Common.Conflict, "Bu admin kullanıcı adı zaten mevcut.");

        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            // Kullanıcıyı oluştur (düz metin parola)
            var user = new User
            {
                UserID = userDto.UserID,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                PhoneNumber = userDto.PhoneNumber,
                DepartmentID = userDto.DepartmentID,
                Role = "Admin",
                IsActive = userDto.IsActive,
                Password = userDto.Password
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Admin kaydı
            var admin = new Admin
            {
                UserID = user.UserID,
                Username = adminDto.Username,
                DepartmentName = adminDto.DepartmentName
            };
            _context.Admins.Add(admin);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAdminDepartmentAsync(long adminId, string newDepartmentName)
    {
        if (string.IsNullOrWhiteSpace(newDepartmentName))
            throw new AppException(AppErrors.Validation.BadRequest, "Department boş olamaz.");

        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminID == adminId);
        if (admin is null) return false;

        admin.DepartmentName = newDepartmentName;
        return await _context.SaveChangesAsync() > 0;
    }
}

