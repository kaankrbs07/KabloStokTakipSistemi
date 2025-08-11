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

    public UserService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<GetUserDto>> GetAllUsersAsync()
    {
        // sp_GetAllUsers: Users + Department join dönüyorsa harika; yoksa Include ile de olur.
        var result = await _context.Users
            .FromSqlRaw("EXEC sp_GetAllUsers")
            .Include(u => u.Department)
            .AsNoTracking()
            .ToListAsync();

        return _mapper.Map<IEnumerable<GetUserDto>>(result);
    }

    public async Task<GetUserDto?> GetUserByIdAsync(long userId)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.UserID == userId);

        return user == null ? null : _mapper.Map<GetUserDto>(user);
    }

    public async Task<bool> CreateUserAsync(CreateUserDto dto)
    {
        try
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

                // Admin ise değer, değilse NULL gönder
                new SqlParameter("@AdminUsername", (object?)(
                    string.Equals(dto.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                        ? dto.AdminUsername
                        : null) ?? DBNull.Value),

                new SqlParameter("@AdminDepartmentName", (object?)(
                    string.Equals(dto.Role, "Admin", StringComparison.OrdinalIgnoreCase)
                        ? dto.AdminDepartmentName
                        : null) ?? DBNull.Value)
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_CreateUser @UserID, @FirstName, @LastName, @Email, @PhoneNumber, @DepartmentID, @Role, @IsActive, @Password, @AdminUsername, @AdminDepartmentName",
                p);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }


    public async Task<bool> UpdateUserAsync(UpdateUserDto dto)
    {
        try
        {
            // Role veya Password geldiyse güvenlik alanlarını da güncelleyen SP’yi çalıştır.
            if (!string.IsNullOrWhiteSpace(dto.Role) || !string.IsNullOrWhiteSpace(dto.Password))
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
                    new SqlParameter("@Role", (object?)dto.Role ?? DBNull.Value),
                    new SqlParameter("@Password", (object?)dto.Password ?? DBNull.Value)
                };

                // Bu SP’yi senin tarafta oluştur: Role/Password null ise dokunmasın.
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_UpdateUsers @UserID, @FirstName, @LastName, @Email, @PhoneNumber, @DepartmentID, @IsActive, @Role, @Password",
                    p);
            }
            else
            {
                var p = new[]
                {
                    new SqlParameter("@UserID", dto.UserID),
                    new SqlParameter("@FirstName", (object?)dto.FirstName ?? DBNull.Value),
                    new SqlParameter("@LastName", (object?)dto.LastName ?? DBNull.Value),
                    new SqlParameter("@Email", (object?)dto.Email ?? DBNull.Value),
                    new SqlParameter("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value),
                    new SqlParameter("@DepartmentID", (object?)dto.DepartmentID ?? DBNull.Value),
                    new SqlParameter("@IsActive", (object?)dto.IsActive ?? DBNull.Value)
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.sp_UpdateUsers @UserID, @FirstName, @LastName, @Email, @PhoneNumber, @DepartmentID, @Role, @IsActive, @Password",
                    p);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeactivateUserAsync(long userId)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_DeactivateUser @UserID",
                new SqlParameter("@UserID", userId));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserActivitySummaryDto?> GetUserActivitySummaryAsync(long userId)
    {
        return await _context.UserActivitySummary
            .FromSqlRaw("EXEC sp_GetUserActivitySummary @UserID", new SqlParameter("@UserID", userId))
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }
}