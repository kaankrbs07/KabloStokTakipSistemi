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
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, IMapper mapper, ILogger<UserService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<GetUserDto>> GetAllUsersAsync()
    {
        try
        {
            _logger.LogInformation("Getting all users from database");
            // sp_GetAllUsers: Users + Department join dönüyorsa harika; yoksa Include ile de olur.
            var result = await _context.Users
                .FromSqlRaw("EXEC sp_GetAllUsers")
                .Include(u => u.Department)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} users from database", result.Count);
            return _mapper.Map<IEnumerable<GetUserDto>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users from database");
            throw;
        }
    }

    public async Task<GetUserDto?> GetUserByIdAsync(long userId)
    {
        try
        {
            _logger.LogInformation("Getting user by ID: {UserId}", userId);
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", userId);
                return null;
            }

            _logger.LogInformation("Retrieved user with ID: {UserId}", userId);
            return _mapper.Map<GetUserDto>(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> CreateUserAsync(CreateUserDto dto)
    {
        try
        {
            _logger.LogInformation("Creating user with ID: {UserId}, Role: {Role}", dto.UserID, dto.Role);
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

            _logger.LogInformation("Successfully created user with ID: {UserId}", dto.UserID);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with ID: {UserId}", dto.UserID);
            return false;
        }
    }


    public async Task<bool> UpdateUserAsync(UpdateUserDto dto)
    {
        try
        {
            _logger.LogInformation("Updating user with ID: {UserId}", dto.UserID);
            // Role veya Password geldiyse güvenlik alanlarını da güncelleyen SP'yi çalıştır.
            if (!string.IsNullOrWhiteSpace(dto.Role) || !string.IsNullOrWhiteSpace(dto.Password))
            {
                _logger.LogInformation("Updating user with security fields (Role/Password) for user ID: {UserId}", dto.UserID);
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

                // Bu SP'yi senin tarafta oluştur: Role/Password null ise dokunmasın.
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_UpdateUsers @UserID, @FirstName, @LastName, @Email, @PhoneNumber, @DepartmentID, @IsActive, @Role, @Password",
                    p);
            }
            else
            {
                _logger.LogInformation("Updating user without security fields for user ID: {UserId}", dto.UserID);
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

            _logger.LogInformation("Successfully updated user with ID: {UserId}", dto.UserID);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID: {UserId}", dto.UserID);
            return false;
        }
    }

    public async Task<bool> DeactivateUserAsync(long userId)
    {
        try
        {
            _logger.LogInformation("Deactivating user with ID: {UserId}", userId);
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_DeactivateUser @UserID",
                new SqlParameter("@UserID", userId));
            _logger.LogInformation("Successfully deactivated user with ID: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user with ID: {UserId}", userId);
            return false;
        }
    }

    public async Task<UserActivitySummaryDto?> GetUserActivitySummaryAsync(long userId)
    {
        try
        {
            _logger.LogInformation("Getting user activity summary for user ID: {UserId}", userId);
            var result = await _context.UserActivitySummary
                .FromSqlRaw("EXEC sp_GetUserActivitySummary @UserID", new SqlParameter("@UserID", userId))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (result == null)
            {
                _logger.LogWarning("User activity summary not found for user ID: {UserId}", userId);
                return null;
            }

            _logger.LogInformation("Retrieved user activity summary for user ID: {UserId}", userId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity summary for user ID: {UserId}", userId);
            throw;
        }
    }
}