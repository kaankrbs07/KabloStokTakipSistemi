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
    private readonly ILogger<EmployeeService> _logger;
    
    public EmployeeService(AppDbContext context, ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<GetEmployeeDto>> GetAllEmployeesAsync()
    {
        try
        {
            _logger.LogInformation("Getting all employees from database");
            var list = await _context.Employees
                .Include(e => e.User)
                .ThenInclude(u => u!.Department)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} employees from database", list.Count);
            return list.Select(e => new GetEmployeeDto(
                e.EmployeeID,
                e.User?.FirstName,
                e.User?.LastName,
                e.User?.Department?.DepartmentName
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all employees from database");
            throw;
        }
    }

    public async Task<GetEmployeeDto?> GetEmployeeByIdAsync(long employeeId)
    {
        try
        {
            _logger.LogInformation("Getting employee by ID: {EmployeeId}", employeeId);
            var e = await _context.Employees
                .Include(x => x.User)
               .ThenInclude(u => u!.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployeeID == employeeId);

            if (e is null)
            {
                _logger.LogWarning("Employee not found with ID: {EmployeeId}", employeeId);
                return null;
            }

            _logger.LogInformation("Retrieved employee with ID: {EmployeeId}", employeeId);
            return new GetEmployeeDto(
                e.EmployeeID,
                e.User?.FirstName,
                e.User?.LastName,
                e.User?.Department?.DepartmentName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee by ID: {EmployeeId}", employeeId);
            throw;
        }
    }

    // Not: sp_CreateUser, Role='Employee' geldiğinde Employees tablosuna da INSERT etmeli.
    public async Task<bool> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Creating employee with ID: {EmployeeId}, UserID: {UserId}", dto.EmployeeID, dto.UserID);
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
            _logger.LogInformation("Successfully created employee with ID: {EmployeeId}", dto.EmployeeID);
            return true;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Error creating employee with ID: {EmployeeId}", dto.EmployeeID);
            return false;
        }
    }

    // Department'ı Users tablosunda değiştiriyoruz (Employee->User join)
    public async Task<bool> UpdateEmployeeDepartmentAsync(long employeeId, int newDepartmentId)
{
    try
    {
        _logger.LogInformation("Updating department for employee ID: {EmployeeId} to department ID: {DepartmentId}",
            employeeId, newDepartmentId);

        // 1) Department var mı? Yoksa erken çık.
        var deptExists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.DepartmentID == newDepartmentId);

        if (!deptExists)
        {
            _logger.LogWarning("Department not found with ID: {DepartmentId}", newDepartmentId);
            return false;
        }

        // Employee var mı?
        var emp = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

        if (emp is null)
        {
            _logger.LogWarning("Employee not found with ID: {EmployeeId}", employeeId);
            return false;
        }

        // sp_UpdateUsers: NULL verilen alanlara dokunmuyor.
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

        _logger.LogInformation("Successfully updated department for employee ID: {EmployeeId}", employeeId);
        return true;
    }
    catch (Exception ex)
    {
        // 2) İstisna fırlatmak yerine logla ve false dön (tutarlı bool sözleşmesi).
        _logger.LogError(ex, "Error updating department for employee ID: {EmployeeId}", employeeId);
        return false;
    }
}

}