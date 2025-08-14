
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Middlewares; // AppException/AppErrors

namespace KabloStokTakipSistemi.Services.Implementations;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    // ILogger enjekte kalsın ama bilgi logları kaldırıldı. Gerekirse Warning için kullanırız.
    private readonly ILogger<AdminService> _logger;
    public AdminService(AppDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger  = logger;
    }

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

        if (a is null) return null; // Controller 404'a çevirir (middleware 4xx'ü Warning olarak yazar)

        return new GetAdminDto(
            a.AdminID,
            a.Username,
            a.DepartmentName,
            a.User?.FirstName,
            a.User?.LastName
        );
    }

    public async Task<bool> CreateAdminAsync(CreateUserDto userDto, CreateAdminDto adminDto)
    {
        // Basit validasyon – ayrıntı yerine kod dönmesi için AppException fırlat
        if (string.IsNullOrWhiteSpace(adminDto.Username))
            throw new AppException(AppErrors.Validation.BadRequest, "Admin username boş olamaz.");

        var parameters = new[]
        {
            new SqlParameter("@UserID", userDto.UserID),
            new SqlParameter("@FirstName", (object?)userDto.FirstName ?? DBNull.Value),
            new SqlParameter("@LastName", (object?)userDto.LastName ?? DBNull.Value),
            new SqlParameter("@Email", (object?)userDto.Email ?? DBNull.Value),
            new SqlParameter("@PhoneNumber", (object?)userDto.PhoneNumber ?? DBNull.Value),
            new SqlParameter("@DepartmentID", (object?)userDto.DepartmentID ?? DBNull.Value),
            new SqlParameter("@Role", "Admin"),
            new SqlParameter("@IsActive", userDto.IsActive),
            new SqlParameter("@Password", userDto.Password),
            new SqlParameter("@AdminUsername", adminDto.Username),
            new SqlParameter("@AdminDepartmentName", adminDto.DepartmentName)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_CreateUser @UserID, @FirstName, @LastName, @Email, @PhoneNumber, " +
            "@DepartmentID, @Role, @IsActive, @Password, @AdminUsername, @AdminDepartmentName",
            parameters
        );

        return true;
    }

    public async Task<bool> UpdateAdminDepartmentAsync(long adminId, string newDepartmentName)
    {
        if (string.IsNullOrWhiteSpace(newDepartmentName))
            throw new AppException(AppErrors.Validation.BadRequest, "Department boş olamaz.");

        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminID == adminId);
        if (admin is null) return false; // Controller 404'a çevirsin

        admin.DepartmentName = newDepartmentName;
        _context.Admins.Update(admin);

        return await _context.SaveChangesAsync() > 0;
    }
}
