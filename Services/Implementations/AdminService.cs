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
    // --- Validations ---
    if (string.IsNullOrWhiteSpace(adminDto.Username))
        throw new AppException(AppErrors.Validation.BadRequest, "Admin username boş olamaz.");
    if (string.IsNullOrWhiteSpace(adminDto.DepartmentName))
        throw new AppException(AppErrors.Validation.BadRequest, "Admin department boş olamaz.");
    if (string.IsNullOrWhiteSpace(userDto.Password))
        throw new AppException(AppErrors.Validation.BadRequest, "Parola boş olamaz.");

    // Username benzersizliği
    var usernameExists = await _context.Admins.AsNoTracking()
        .AnyAsync(a => a.Username == adminDto.Username);
    if (usernameExists)
        throw new AppException(AppErrors.Common.Conflict, "Bu admin kullanıcı adı zaten mevcut.");

    // Department'ı isimden bul
    var dept = await _context.Departments.AsNoTracking()
        .Where(d => d.DepartmentName == adminDto.DepartmentName.Trim())
        .Select(d => new { d.DepartmentID, d.DepartmentName })
        .FirstOrDefaultAsync();

    if (dept is null)
        throw new AppException(AppErrors.Validation.BadRequest, "Böyle bir departman bulunamadı.");

    // --- Transaction (yarışları engelle) ---
    await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    try
    {
        // Aynı departmanda admin var mı? (TX içinde kontrol)
        var deptHasAdmin = await _context.Admins.AsNoTracking()
            .AnyAsync(a => a.DepartmentName == dept.DepartmentName);
        if (deptHasAdmin)
            throw new AppException(AppErrors.Common.Conflict, "Bu departmanın zaten bir admini var.");

        // (Opsiyonel) Dışarıdan UserID verildiyse çakışma kontrolü
        if (userDto.UserID > 0)
        {
            var userExists = await _context.Users.AsNoTracking()
                .AnyAsync(u => u.UserID == userDto.UserID);
            if (userExists)
                throw new AppException(AppErrors.Common.Conflict, "Bu UserID zaten mevcut.");
        }

        // Kullanıcı kaydı (PAROLA DÜZ METİN)
        var user = new User
        {
            FirstName    = userDto.FirstName,
            LastName     = userDto.LastName,
            Email        = userDto.Email,
            PhoneNumber  = userDto.PhoneNumber,
            DepartmentID = dept.DepartmentID,      // Admin için zorunlu
            Role         = "Admin",
            IsActive     = userDto.IsActive,
            Password     = userDto.Password        // <-- Plain text (isteğin üzerine)
        };
        if (userDto.UserID > 0)
            user.UserID = userDto.UserID;         // Aksi halde DB/EF üretsin

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Admin kaydı
        var admin = new Admin
        {
            UserID         = user.UserID,
            Username       = adminDto.Username.Trim(),
            DepartmentName = dept.DepartmentName
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

