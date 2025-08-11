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
            a.DepartmentName,           // Admins tablosu
            a.User?.FirstName,          // Users tablosu
            a.User?.LastName            // Users tablosu
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
        try
        {
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

            return true;
        }
        catch (Exception ex)
        {
            // Hata loglama eklenebilir
            return false;
        }
    }


    public async Task<bool> UpdateAdminDepartmentAsync(long adminId, string newDepartmentName)
    {
        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminID == adminId);
        if (admin is null) return false;

        admin.DepartmentName = newDepartmentName;
        _context.Admins.Update(admin);

        return await _context.SaveChangesAsync() > 0;
    }
}

