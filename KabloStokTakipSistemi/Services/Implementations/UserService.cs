using AutoMapper;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Middlewares;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    // DI imzasını koruyalım; logger enjekte kalsın ama kullanılmıyor
    public UserService(AppDbContext context, IMapper mapper, ILogger<UserService> _)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<GetUserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .FromSqlRaw("EXEC sp_GetAllUsers")
            .Include(u => u.Department)
            .AsNoTracking()
            .ToListAsync();

        return _mapper.Map<IEnumerable<GetUserDto>>(users);
    }

    public async Task<GetUserDto?> GetUserByIdAsync(long userId)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserID == userId);

        return user is null ? null : _mapper.Map<GetUserDto>(user);
    }
 
    Task<bool> IUserService.CreateUserAsync(CreateUserDto dto)
        => CreateUserAsync(dto);
    
public async Task<bool> CreateUserAsync(CreateUserDto dto)
{
    // --- Role doğrulama ---
    var role = (dto.Role ?? "").Trim();
    var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    var isEmployee = role.Equals("Employee", StringComparison.OrdinalIgnoreCase);
    if (!isAdmin && !isEmployee)
        throw new AppException(AppErrors.Validation.BadRequest, "Role sadece 'Employee' veya 'Admin' olabilir.");

    // --- DepartmentID çözümleme ---
    int? resolvedDeptId = dto.DepartmentID;

    if (isAdmin)
    {
        // Admin için ek alanlar
        if (string.IsNullOrWhiteSpace(dto.AdminUsername))
            throw new AppException(AppErrors.Validation.BadRequest, "AdminUsername zorunludur.");
        if (string.IsNullOrWhiteSpace(dto.AdminDepartmentName) && resolvedDeptId is null)
            throw new AppException(AppErrors.Validation.BadRequest, "AdminDepartmentName veya DepartmentID zorunludur.");

        // DepartmentID gelmediyse, DepartmentName'den bul
        if (resolvedDeptId is null && !string.IsNullOrWhiteSpace(dto.AdminDepartmentName))
        {
            var name = dto.AdminDepartmentName.Trim();

            resolvedDeptId = await _context.Departments
                .Where(d => d.DepartmentName == name)
                .Select(d => (int?)d.DepartmentID)
                .FirstOrDefaultAsync();

            if (resolvedDeptId is null)
                throw new AppException(AppErrors.Common.NotFound, "Seçilen departman bulunamadı.");
        }
    }

    // Employee için ve genel durumda DepartmentID şart
    if (resolvedDeptId is null)
        throw new AppException(AppErrors.Validation.BadRequest, "DepartmentID zorunludur.");
    
    var p = new[]
    {
        new SqlParameter("@FirstName", (object?)dto.FirstName ?? DBNull.Value),
        new SqlParameter("@LastName", (object?)dto.LastName ?? DBNull.Value),
        new SqlParameter("@Email", (object?)dto.Email ?? DBNull.Value),
        new SqlParameter("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value),
        new SqlParameter("@DepartmentID", resolvedDeptId.Value),
        new SqlParameter("@Role", role),
        new SqlParameter("@Password", dto.Password),

        // Admin ise dolu; Employee ise NULL
        new SqlParameter("@AdminUsername", (object?)(isAdmin ? dto.AdminUsername : null) ?? DBNull.Value),
        new SqlParameter("@AdminDepartmentName", (object?)(isAdmin ? dto.AdminDepartmentName : null) ?? DBNull.Value),
    };

    await _context.Database.ExecuteSqlRawAsync(
        "EXEC dbo.sp_CreateUser @FirstName, @LastName, @Email, @PhoneNumber, " +
        "@DepartmentID, @Role, @Password, @AdminUsername, @AdminDepartmentName",
        p
    );


    return true;
}


    public async Task<bool> UpdateUserAsync(UpdateUserDto dto)
    {
        var p = new[]
        {
            new SqlParameter("@UserID", dto.UserID),
            new SqlParameter("@FirstName", (object?)dto.FirstName ?? DBNull.Value),
            new SqlParameter("@LastName", (object?)dto.LastName ?? DBNull.Value),
            new SqlParameter("@Email", (object?)dto.Email ?? DBNull.Value),
            new SqlParameter("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value),
            new SqlParameter("@DepartmentID", (object?)dto.DepartmentID ?? DBNull.Value),

            // Security alanları: boş/whitespace ise NULL gönder.
            new SqlParameter("@Role", string.IsNullOrWhiteSpace(dto.Role) ? (object)DBNull.Value : dto.Role),
            new SqlParameter("@Password", string.IsNullOrWhiteSpace(dto.Password) ? (object)DBNull.Value : dto.Password)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_UpdateUsers @UserID, @FirstName, @LastName, @Email, @PhoneNumber, @DepartmentID, @Role, @Password",
            p);

        return true;
    }

    public async Task<bool> DeactivateUserAsync(long userId)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_DeactivateUser @UserID",
            new SqlParameter("@UserID", userId));

        return true;
    }

    public async Task<UserActivitySummaryDto?> GetUserActivitySummaryAsync(long userId)
    {
        // sp_GetUserActivitySummary kullanıcıya ait aktivite özetini döner
        return await _context.UserActivitySummary
            .FromSqlRaw("EXEC sp_GetUserActivitySummary @UserID", new SqlParameter("@UserID", userId))
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }
}
