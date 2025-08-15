using System.Data;
using AutoMapper;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Middlewares; // AppException, AppErrors
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    // ILogger kaldırıldı — merkezi NLog + middleware kullanılıyor
    public UserService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    // === CREATE ===========================================================
    public async Task<bool> CreateUserAsync(CreateUserDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        // Role doğrulama
        var isAdmin = string.Equals(dto.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        var isEmployee = string.Equals(dto.Role, "Employee", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !isEmployee)
            throw new AppException(AppErrors.Validation.BadRequest, "Geçersiz Role.");

        // Duplicate kontrolü
        var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.UserID == dto.UserID);
        if (exists)
            throw new AppException(AppErrors.Common.Conflict, "Bu UserID zaten mevcut."); // 409

        // ŞİFRE HASHLEME YOK — düz metin yazılacak (geçici/debug için)
        var user = new User
        {
            UserID = dto.UserID,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            DepartmentID = dto.DepartmentID,
            Role = dto.Role,
            IsActive = dto.IsActive,
            Password = dto.Password
        };

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            if (isAdmin)
            {
                if (string.IsNullOrWhiteSpace(dto.AdminUsername) ||
                    string.IsNullOrWhiteSpace(dto.AdminDepartmentName))
                    throw new AppException(AppErrors.Validation.BadRequest,
                        "Admin için Username ve DepartmentName zorunlu.");

                _db.Admins.Add(new Admin
                {
                    UserID = user.UserID,
                    Username = dto.AdminUsername!,
                    DepartmentName = dto.AdminDepartmentName!
                });
            }
            else // Employee
            {
                _db.Employees.Add(new Employee { UserID = user.UserID });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // === UPDATE ==========================================================
    public async Task<bool> UpdateUserAsync(UpdateUserDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == dto.UserID);
        if (user is null) return false;

        if (dto.FirstName != null) user.FirstName = dto.FirstName;
        if (dto.LastName != null) user.LastName = dto.LastName;
        if (dto.Email != null) user.Email = dto.Email;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
        if (dto.DepartmentID.HasValue) user.DepartmentID = dto.DepartmentID;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;

        // ŞİFRE HASHLEME YOK — düz metin
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.Password = dto.Password;

        // Rol değişimi tutarlılığı
        if (!string.IsNullOrWhiteSpace(dto.Role) &&
            !dto.Role.Equals(user.Role, StringComparison.OrdinalIgnoreCase))
        {
            var newIsAdmin = dto.Role!.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            var newIsEmployee = dto.Role!.Equals("Employee", StringComparison.OrdinalIgnoreCase);
            if (!newIsAdmin && !newIsEmployee)
                throw new AppException(AppErrors.Validation.BadRequest, "Geçersiz Role.");

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                user.Role = dto.Role!;

                if (newIsAdmin)
                {
                    // Admin’e alırken Admin kaydı var mı?
                    var hasAdmin = await _db.Admins.AnyAsync(a => a.UserID == user.UserID);
                    if (!hasAdmin)
                        throw new AppException(AppErrors.Validation.BadRequest,
                            "Rolü Admin'e almak için önce Admin kaydı oluşturun.");
                    var emp = await _db.Employees.FirstOrDefaultAsync(e => e.UserID == user.UserID);
                    if (emp != null) _db.Employees.Remove(emp);
                }
                else // Employee
                {
                    var admin = await _db.Admins.FirstOrDefaultAsync(a => a.UserID == user.UserID);
                    if (admin != null) _db.Admins.Remove(admin);
                    var hasEmp = await _db.Employees.AnyAsync(e => e.UserID == user.UserID);
                    if (!hasEmp) _db.Employees.Add(new Employee { UserID = user.UserID });
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        else
        {
            await _db.SaveChangesAsync();
        }

        return true;
    }

    // === DEACTIVATE ======================================================
    public async Task<bool> DeactivateUserAsync(long userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);
        if (user is null) return false;
        if (!user.IsActive) return true;
        user.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    // === QUERIES =========================================================
    public async Task<IEnumerable<GetUserDto>> GetAllUsersAsync()
    {
        var users = await _db.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .ToListAsync();

        return _mapper.Map<IEnumerable<GetUserDto>>(users);
    }

    public async Task<GetUserDto?> GetUserByIdAsync(long userId)
    {
        var user = await _db.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserID == userId);

        return user is null ? null : _mapper.Map<GetUserDto>(user);
    }

    // İsteğe göre doldur: örnek bir özet fonksiyonu (eğer Log tablosu/DTO’n var ise)
    public async Task<UserActivitySummaryDto?> GetUserActivitySummaryAsync(long userId)
    {
        return await _db.UserActivitySummary
            .FromSqlRaw("EXEC sp_GetUserActivitySummary @UserID", new SqlParameter("@UserID", userId))
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }
    }

