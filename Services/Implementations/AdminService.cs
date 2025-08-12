// Services/AdminService.cs
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminService> _logger;
    
    public AdminService(AppDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<GetAdminDto>> GetAllAdminsAsync()
    {
        try
        {
            _logger.LogInformation("Getting all admins from database");
            var admins = await _context.Admins
                .Include(a => a.User)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} admins from database", admins.Count);
            return admins.Select(a => new GetAdminDto(
                a.AdminID,
                a.Username,
                a.DepartmentName,           // Admins tablosu
                a.User?.FirstName,          // Users tablosu
                a.User?.LastName            // Users tablosu
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all admins from database");
            throw;
        }
    }

    public async Task<GetAdminDto?> GetAdminByIdAsync(long adminId)
    {
        try
        {
            _logger.LogInformation("Getting admin by ID: {AdminId}", adminId);
            var a = await _context.Admins
                .Include(x => x.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AdminID == adminId);

            if (a is null)
            {
                _logger.LogWarning("Admin not found with ID: {AdminId}", adminId);
                return null;
            }

            _logger.LogInformation("Retrieved admin with ID: {AdminId}", adminId);
            return new GetAdminDto(
                a.AdminID,
                a.Username,
                a.DepartmentName,
                a.User?.FirstName,
                a.User?.LastName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin by ID: {AdminId}", adminId);
            throw;
        }
    }

    public async Task<bool> CreateAdminAsync(CreateUserDto userDto, CreateAdminDto adminDto)
    {
        try
        {
            _logger.LogInformation("Creating admin with UserID: {UserId}, Username: {Username}", userDto.UserID, adminDto.Username);
            var parameters = new[]
            {
                new SqlParameter("@UserID", userDto.UserID),
                new SqlParameter("@FirstName", (object?)userDto.FirstName ?? DBNull.Value),
                new SqlParameter("@LastName", (object?)userDto.LastName ?? DBNull.Value),
                new SqlParameter("@Email", (object?)userDto.Email ?? DBNull.Value),
                new SqlParameter("@PhoneNumber", (object?)userDto.PhoneNumber ?? DBNull.Value),
                new SqlParameter("@DepartmentID", (object?)userDto.DepartmentID ?? DBNull.Value),
                new SqlParameter("@Role", "Admin"), // zorunlu admin rolü
                new SqlParameter("@IsActive", userDto.IsActive),
                new SqlParameter("@Password", userDto.Password),

                // Admin tablosuna özel alanlar
                new SqlParameter("@AdminUsername", adminDto.Username),
                new SqlParameter("@AdminDepartmentName", adminDto.DepartmentName)
            };

            // Yeni SP: hem Users'a hem Admins'e ekleme yapar
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_CreateUser @UserID, @FirstName, @LastName, @Email, @PhoneNumber, " +
                "@DepartmentID, @Role, @IsActive, @Password, @AdminUsername, @AdminDepartmentName",
                parameters
            );

            _logger.LogInformation("Successfully created admin with UserID: {UserId}", userDto.UserID);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin with UserID: {UserId}", userDto.UserID);
            return false;
        }
    }


    public async Task<bool> UpdateAdminDepartmentAsync(long adminId, string newDepartmentName)
    {
        try
        {
            _logger.LogInformation("Updating department for admin ID: {AdminId} to: {DepartmentName}", adminId, newDepartmentName);
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminID == adminId);
            
            if (admin is null)
            {
                _logger.LogWarning("Admin not found with ID: {AdminId}", adminId);
                return false;
            }

            admin.DepartmentName = newDepartmentName;
            _context.Admins.Update(admin);

            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                _logger.LogInformation("Successfully updated department for admin ID: {AdminId}", adminId);
            }
            else
            {
                _logger.LogWarning("Failed to save changes for admin ID: {AdminId}", adminId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department for admin ID: {AdminId}", adminId);
            throw;
        }
    }
}

