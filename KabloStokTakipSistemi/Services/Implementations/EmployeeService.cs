
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

    public EmployeeService(AppDbContext context, ILogger<EmployeeService> _ /*logger enjekte edilebilir*/)
    {
        _context = context;
    }

    public async Task<IEnumerable<GetEmployeeDto>> GetAllEmployeesAsync()
    {
        var list = await _context.Employees
            .Include(e => e.User)
            .ThenInclude(u => u!.Department)
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
            .ThenInclude(u => u!.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmployeeID == employeeId);

        return e is null
            ? null
            : new GetEmployeeDto(
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
                new SqlParameter("@FirstName", (object?)dto.FirstName ?? DBNull.Value),
                new SqlParameter("@LastName", (object?)dto.LastName ?? DBNull.Value),
                new SqlParameter("@Email", (object?)dto.Email ?? DBNull.Value),
                new SqlParameter("@PhoneNumber", (object?)dto.PhoneNumber ?? DBNull.Value),
                new SqlParameter("@DepartmentID", (object?)dto.DepartmentID ?? DBNull.Value),
                new SqlParameter("@Role", "Employee"),
                new SqlParameter("@IsActive", true),
                new SqlParameter("@Password", dto.Password),
                new SqlParameter("@AdminUsername", DBNull.Value),
                new SqlParameter("@AdminDepartmentName", DBNull.Value)
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_CreateUser @FirstName, @LastName, @Email, @PhoneNumber, " +
                "@DepartmentID, @Role, @IsActive, @Password, @AdminUsername, @AdminDepartmentName",
                p
            );

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw; // Middleware 5xx + hata kodu dönecek, NLog Error yazacak
        }
    }

    // Department'ı Users tablosunda değiştiriyoruz (Employee->User join)
    public async Task<bool> UpdateEmployeeDepartmentAsync(long employeeId, int newDepartmentId)
    {
        // 1) Department var mı?
        var deptExists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.DepartmentID == newDepartmentId);

        if (!deptExists) return false;

        // 2) Employee var mı?
        var emp = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

        if (emp is null) return false;

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
            "EXEC dbo.sp_UpdateUsers @UserID, @FirstName, @LastName, @Email, @PhoneNumber, @DepartmentID, @IsActive, @Role, @Password",
            p);

        return true;
    }
}
