
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _context;
    public EmployeeService(AppDbContext context) => _context = context;

    public async Task<IEnumerable<GetEmployeeDto>> GetAllEmployeesAsync()
    {
        var list = await _context.Employees
            .Include(e => e.User)
            .ThenInclude(u => u.Department)
            .AsNoTracking()
            .ToListAsync();

        return list.Select(e => new GetEmployeeDto(
            e.EmployeeID,
            e.User?.FirstName,
            e.User?.LastName,
            e.User?.Department?.DepartmentName
        ));
    }

    public async Task<GetEmployeeDto?> GetEmployeeByIdAsync(long employeeId)
    {
        var e = await _context.Employees
            .Include(x => x.User)
            .ThenInclude(u => u.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmployeeID == employeeId);

        if (e is null) return null;

        return new GetEmployeeDto(
            e.EmployeeID,
            e.User?.FirstName,
            e.User?.LastName,
            e.User?.Department?.DepartmentName
        );
    }

    // Not: sp_CreateUser, Role='Employee' geldiğinde Employees tablosuna da INSERT etmeli.
    public async Task<bool> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
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
                new SqlParameter("@Role", "Employee"),
                new SqlParameter("@IsActive", true),
                new SqlParameter("@Password", dto.Password),

                // Admin parametreleri SP imzasında varsa NULL geçelim
                new SqlParameter("@AdminUsername", DBNull.Value),
                new SqlParameter("@AdminDepartmentName", DBNull.Value)
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_CreateUser @UserID, @FirstName, @LastName, @Email, @PhoneNumber, " +
                "@DepartmentID, @Role, @IsActive, @Password, @AdminUsername, @AdminDepartmentName",
                p
            );

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }

    // Department’ı Users tablosunda değiştiriyoruz (Employee->User join)
    public async Task<bool> UpdateEmployeeDepartmentAsync(long employeeId, int newDepartmentId)
    {
        var emp = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

        if (emp is null) return false;

        // sp_UpdateUsers: NULL gelen alanlara dokunmaz (öyle tasarladık)
        var p = new[]
        {
            new SqlParameter("@UserID", emp.UserID),
            new SqlParameter("@FirstName", DBNull.Value),
            new SqlParameter("@LastName", DBNull.Value),
            new SqlParameter("@Email", DBNull.Value),
            new SqlParameter("@PhoneNumber", DBNull.Value),
            new SqlParameter("@DepartmentID", newDepartmentId),
            new SqlParameter("@IsActive", DBNull.Value),
            new SqlParameter("@Role", DBNull.Value),
            new SqlParameter("@Password", DBNull.Value)
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_UpdateUsers @UserID, @FirstName, @LastName, @Email, @PhoneNumber, " +
            "@DepartmentID, @IsActive, @Role, @Password",
            p
        );

        return true;
    }
}

