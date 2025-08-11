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
            var parameters = new[]
            {
                new SqlParameter("@UserID", dto.UserID),
                new SqlParameter("@FirstName", (object?)dto.FirstName ?? DBNull.Value),
                new SqlParameter("@LastName", (object?)dto.LastName ?? DBNull.Value),
                new SqlParameter("@Email", (object?)dto.Email ?? DBNull.Value),
                new SqlParameter("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value),
                new SqlParameter("@DepartmentID", (object?)dto.DepartmentID ?? DBNull.Value),
                new SqlParameter("@Role", dto.Role),
                new SqlParameter("@IsActive", dto.IsActive),
                new SqlParameter("@Password", dto.Password)
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_CreateUser @UserID, @FirstName, @LastName, @Email, @PhoneNumber, @DepartmentID, @Role, @IsActive, @Password",
                parameters);

            return true;
        }
        catch (Exception ex)
        {
            // loglama yapılabilir
            return false;
        }
    }

    public async Task<bool> UpdateUserAsync(UpdateUserDto dto)
    {
        try
        {
            var parameters = new[]
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
                "EXEC sp_UpdateUsers @UserID, @FirstName, @LastName, @Email, @PhoneNumber, @DepartmentID, @IsActive",
                parameters);

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
            var param = new SqlParameter("@UserID", userId);
            await _context.Database.ExecuteSqlRawAsync("EXEC sp_DeactivateUser @UserID", param);
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

