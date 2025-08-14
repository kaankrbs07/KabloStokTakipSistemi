using AutoMapper;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
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

    public async Task<bool> CreateUserAsync(CreateUserDto dto)
    {
        var p = new[]
        {
            new SqlParameter("@UserID", dto.UserID),
            new SqlParameter("@FirstName", (object?)dto.FirstName ?? DBNull.Value),
            new SqlParameter("@LastName", (object?)dto.LastName ?? DBNull.Value),
            new SqlParameter("@Email", (object?)dto.Email ?? DBNull.Value),
            new SqlParameter("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value),
            new SqlParameter("@DepartmentID", (object?)dto.DepartmentID ?? DBNull.Value),
            new SqlParameter("@Role", dto.Role),
            new SqlParameter("@IsActive", dto.IsActive),
            new SqlParameter("@Password", dto.Password),

            // Admin ise değer, değilse NULL
            new SqlParameter("@AdminUsername", (object?)(
                string.Equals(dto.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? dto.AdminUsername : null
            ) ?? DBNull.Value),

            new SqlParameter("@AdminDepartmentName", (object?)(
                string.Equals(dto.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? dto.AdminDepartmentName : null
            ) ?? DBNull.Value)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_CreateUser @UserID, @FirstName, @LastName, @Email, @PhoneNumber, @DepartmentID, @Role, @IsActive, @Password, @AdminUsername, @AdminDepartmentName",
            p);

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
            new SqlParameter("@IsActive", (object?)dto.IsActive ?? DBNull.Value),

            // Security alanları: boş/whitespace ise NULL gönder.
            new SqlParameter("@Role", string.IsNullOrWhiteSpace(dto.Role) ? (object)DBNull.Value : dto.Role),
            new SqlParameter("@Password", string.IsNullOrWhiteSpace(dto.Password) ? (object)DBNull.Value : dto.Password)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_UpdateUsers @UserID, @FirstName, @LastName, @Email, @PhoneNumber, @DepartmentID, @IsActive, @Role, @Password",
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
