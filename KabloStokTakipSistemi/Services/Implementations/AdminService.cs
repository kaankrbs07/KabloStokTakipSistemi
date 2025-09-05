using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Middlewares; 

namespace KabloStokTakipSistemi.Services.Implementations;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(AppDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger  = logger;
    }

    // ---- Tüm adminleri listele ----
    public async Task<IEnumerable<GetAdminDto>> GetAllAdminsAsync()
    {
        var admins = await _context.Admins
            .Include(a => a.User)
            .AsNoTracking()
            .ToListAsync();

        return admins.Select(a => new GetAdminDto(
            a.Username,
            a.DepartmentName,
            a.User?.FirstName,
            a.User?.LastName
        ));
    }

    // ---- Username ile admin getir ----
    public async Task<GetAdminDto?> GetAdminByUsernameAsync(string username)
    {
        var a = await _context.Admins
            .Include(x => x.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username);

        if (a is null) return null; // Controller 404 döner

        return new GetAdminDto(
            a.Username,
            a.DepartmentName,
            a.User?.FirstName,
            a.User?.LastName
        );
    }

    // ---- Admin oluştur (sp_CreateUser üzerinden) ----
    public async Task<bool> CreateAdminAsync(CreateUserDto userDto, CreateAdminDto adminDto)
    {
        if (string.IsNullOrWhiteSpace(adminDto.Username))
            throw new AppException(AppErrors.Validation.BadRequest, "Admin username boş olamaz.");

        int? resolvedDeptId = userDto.DepartmentID;
        if (resolvedDeptId is null)
        {
            if (string.IsNullOrWhiteSpace(adminDto.DepartmentName))
                throw new AppException(AppErrors.Validation.BadRequest, "DepartmentName zorunludur.");

            var name = adminDto.DepartmentName.Trim();
            resolvedDeptId = await _context.Departments
                .Where(d => d.DepartmentName == name)
                .Select(d => (int?)d.DepartmentID)
                .FirstOrDefaultAsync();

            if (resolvedDeptId is null)
                throw new AppException(AppErrors.Common.NotFound, "Seçilen departman bulunamadı.");
        }

        var parameters = new[]
        {
            new SqlParameter("@FirstName", (object?)userDto.FirstName ?? DBNull.Value),
            new SqlParameter("@LastName", (object?)userDto.LastName ?? DBNull.Value),
            new SqlParameter("@Email", (object?)userDto.Email ?? DBNull.Value),
            new SqlParameter("@PhoneNumber", (object?)userDto.PhoneNumber ?? DBNull.Value),
            new SqlParameter("@DepartmentID", resolvedDeptId!.Value),
            new SqlParameter("@Role", "Admin"),
            new SqlParameter("@Password", userDto.Password), // hash servis içinde yapılabilir

            new SqlParameter("@AdminUsername", adminDto.Username),
            new SqlParameter("@AdminDepartmentName", adminDto.DepartmentName)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_CreateUser @FirstName, @LastName, @Email, @PhoneNumber, " +
            "@DepartmentID, @Role, @Password, @AdminUsername, @AdminDepartmentName",
            parameters
        );

        return true;
    }

    // ---- Admin departman güncelle ----
    public async Task<bool> UpdateAdminDepartmentAsync(string username, string newDepartmentName)
    {
        if (string.IsNullOrWhiteSpace(newDepartmentName))
            throw new AppException(AppErrors.Validation.BadRequest, "Department boş olamaz.");

        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
        if (admin is null) return false;

        admin.DepartmentName = newDepartmentName;
        _context.Admins.Update(admin);

        return await _context.SaveChangesAsync() > 0;
    }
}


