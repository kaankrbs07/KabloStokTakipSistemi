using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;

    public AdminService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GetAdminDto>> GetAllAdminsAsync()
    {
        var admins = await _context.Admins
            .Include(a => a.User)
            .ThenInclude(u => u.Department)
            .ToListAsync();

        return admins.Select(a => new GetAdminDto(
            a.AdminID,
            a.Username,
            a.User?.FirstName,
            a.User?.LastName,
            a.User?.Department?.DepartmentName
        ));
    }

    public async Task<GetAdminDto?> GetAdminByIdAsync(long adminId)
    {
        var admin = await _context.Admins
            .Include(a => a.User)
            .ThenInclude(u => u.Department)
            .FirstOrDefaultAsync(a => a.AdminID == adminId);

        if (admin == null) return null;

        return new GetAdminDto(
            admin.AdminID,
            admin.Username,
            admin.User?.FirstName,
            admin.User?.LastName,
            admin.User?.Department?.DepartmentName
        );
    }

    public async Task<bool> CreateAdminAsync(CreateUserDto dto,CreateAdminDto adminDto)
    {
        // 1. Kullanıcıyı oluştur (Users + Admins)
        var result = await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_CreateUser @UserID = {0}, @Password = {1}, @Role = {2}, @DepartmentID = {3}",
            dto.UserID,
            dto.Password,
            "Admin",
            dto.DepartmentID ?? (object)DBNull.Value
        );

        if (result <= 0)
            return false;

        // 2. Admin tablosuna kayıt
        var admin = new Admin
        {
            AdminID = adminDto.AdminID,
            Username =adminDto.Username,
            UserID = dto.UserID
        };

        _context.Admins.Add(admin);
        return await _context.SaveChangesAsync() > 0;
    }


    public async Task<bool> UpdateAdminDepartmentAsync(long adminId, int newDepartmentId)
    {
        var admin = await _context.Admins
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AdminID == adminId);

        if (admin == null || admin.User == null)
            return false;

        admin.User.DepartmentID = newDepartmentId;
        _context.Users.Update(admin.User);

        return await _context.SaveChangesAsync() > 0;
    }
}
