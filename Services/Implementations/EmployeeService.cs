using System.Data;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Middlewares; // AppException/AppErrors
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _context;

    // ILogger kaldırıldı — merkezi NLog + middleware kullanılıyor
    public EmployeeService(AppDbContext context) => _context = context;

    public async Task<IEnumerable<GetEmployeeDto>> GetAllEmployeesAsync()
    {
        var list = await _context.Employees
            .Include(e => e.User)!.ThenInclude(u => u!.Department)
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
            .Include(x => x.User)!.ThenInclude(u => u!.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmployeeID == employeeId);

        return e is null ? null : new GetEmployeeDto(
            e.EmployeeID,
            e.User?.FirstName,
            e.User?.LastName,
            e.User?.Department?.DepartmentName
        );
    }

    public async Task<bool> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        // Department kontrolü (varsa)
        if (dto.DepartmentID.HasValue)
        {
            var deptExists = await _context.Departments.AsNoTracking()
                .AnyAsync(d => d.DepartmentID == dto.DepartmentID.Value);
            if (!deptExists)
                throw new AppException(AppErrors.Validation.BadRequest, "Geçersiz DepartmentID.");
        }

        // Aynı UserID var mı?
        var userExists = await _context.Users.AsNoTracking()
            .AnyAsync(u => u.UserID == dto.UserID);
        if (userExists)
            throw new AppException(AppErrors.Common.Conflict, "Bu UserID zaten mevcut.");

        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            // Kullanıcı oluştur (düz metin parola)
            var user = new User
            {
                UserID = dto.UserID,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                DepartmentID = dto.DepartmentID,
                Role = "Employee",
                IsActive = true,
                Password = dto.Password
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Employee kaydı
            _context.Employees.Add(new Employee { UserID = user.UserID });
            await _context.SaveChangesAsync();

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // Department'ı Users tablosunda güncelle
    public async Task<bool> UpdateEmployeeDepartmentAsync(long employeeId, int newDepartmentId)
    {
        // 1) Department var mı?
        var deptExists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.DepartmentID == newDepartmentId);
        if (!deptExists) return false;

        // 2) Employee + User yükle (tracking)
        var emp = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);
        if (emp is null || emp.User is null) return false;

        emp.User.DepartmentID = newDepartmentId;
        return await _context.SaveChangesAsync() > 0;
    }
}
