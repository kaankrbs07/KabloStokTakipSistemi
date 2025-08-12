// Services/DepartmentService.cs
using AutoMapper;
using AutoMapper.QueryableExtensions;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(AppDbContext db, IMapper mapper, ILogger<DepartmentService> logger)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<GetDepartmentDto?> GetByIdAsync(int departmentId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting department by ID: {DepartmentId}", departmentId);
            var result = await _db.Departments.AsNoTracking()
                .Where(d => d.DepartmentID == departmentId)
                .ProjectTo<GetDepartmentDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(ct);

            if (result == null)
            {
                _logger.LogWarning("Department not found with ID: {DepartmentId}", departmentId);
                return null;
            }

            _logger.LogInformation("Retrieved department with ID: {DepartmentId}", departmentId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting department by ID: {DepartmentId}", departmentId);
            throw;
        }
    }

    public async Task<IReadOnlyList<GetDepartmentDto>> GetAsync(
        int? adminId = null,
        string? search = null,
        int skip = 0,
        int take = 100,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting departments with filters - AdminId: {AdminId}, Search: {Search}, Skip: {Skip}, Take: {Take}", 
                adminId, search, skip, take);
            
            IQueryable<Department> q = _db.Departments.AsNoTracking();

            if (adminId is not null)
                q = q.Where(d => d.AdminID == adminId);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(d => d.DepartmentName != null && d.DepartmentName.Contains(search));

            q = q.OrderBy(d => d.DepartmentName).ThenBy(d => d.DepartmentID)
                 .Skip(skip).Take(take);

            var result = await q.ProjectTo<GetDepartmentDto>(_mapper.ConfigurationProvider).ToListAsync(ct);
            _logger.LogInformation("Retrieved {Count} departments", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting departments with filters");
            throw;
        }
    }

    public async Task<int> CreateAsync(CreateDepartmentDto dto, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating department with name: {DepartmentName}, AdminId: {AdminId}", dto.DepartmentName, dto.AdminID);
            
            // Temel doğrulama (DB'de NULL olsa bile uygulama seviyesinde boş bırakmıyoruz)
            if (string.IsNullOrWhiteSpace(dto.DepartmentName))
            {
                _logger.LogWarning("Department creation failed - DepartmentName is empty");
                throw new ArgumentException("DepartmentName boş olamaz.");
            }
            
            var exists = await _db.Departments.AnyAsync(d => d.DepartmentName == dto.DepartmentName, ct);
            if (exists)
            {
                _logger.LogWarning("Department creation failed - Department name already exists: {DepartmentName}", dto.DepartmentName);
                throw new InvalidOperationException("Bu departman adı zaten mevcut.");
            }

            var entity = new Department
            {
                DepartmentName = dto.DepartmentName.Trim(),
                AdminID = dto.AdminID,
                CreatedAt = DateTime.Now
            };

            _db.Departments.Add(entity);

            // INSERT tetikleyicisi (trg_Log_InsertDepartment) log'u DB tarafında yazacak.
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully created department with ID: {DepartmentId}", entity.DepartmentID);
            return entity.DepartmentID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department with name: {DepartmentName}", dto.DepartmentName);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int departmentId, UpdateDepartmentDto dto, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating department with ID: {DepartmentId}, Name: {DepartmentName}", departmentId, dto.DepartmentName);
            
            var entity = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentID == departmentId, ct);
            if (entity is null)
            {
                _logger.LogWarning("Department not found with ID: {DepartmentId}", departmentId);
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.DepartmentName))
            {
                _logger.LogWarning("Department update failed - DepartmentName is empty for ID: {DepartmentId}", departmentId);
                throw new ArgumentException("DepartmentName boş olamaz.");
            }

            // (Opsiyonel) Aynı isim kontrolü — kendisi hariç
            var nameClash = await _db.Departments
                .AnyAsync(d => d.DepartmentID != departmentId && d.DepartmentName == dto.DepartmentName, ct);
            if (nameClash)
            {
                _logger.LogWarning("Department update failed - Department name already exists: {DepartmentName}", dto.DepartmentName);
                throw new InvalidOperationException("Bu departman adı zaten mevcut.");
            }

            entity.DepartmentName = dto.DepartmentName.Trim();
            entity.AdminID = dto.AdminID;

            // Not: UPDATE için ayrıca trigger yok; Log gerekiyorsa DB tarafına eklenebilir.
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully updated department with ID: {DepartmentId}", departmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department with ID: {DepartmentId}", departmentId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int departmentId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Deleting department with ID: {DepartmentId}", departmentId);
            
            var entity = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentID == departmentId, ct);
            if (entity is null)
            {
                _logger.LogWarning("Department not found for deletion with ID: {DepartmentId}", departmentId);
                return false;
            }

            _db.Departments.Remove(entity);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully deleted department with ID: {DepartmentId}", departmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department with ID: {DepartmentId}", departmentId);
            throw;
        }
    }

    public async Task<bool> ExistsByNameAsync(string departmentName, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(departmentName))
            {
                _logger.LogWarning("Checking department existence with empty name");
                return false;
            }
            
            _logger.LogInformation("Checking if department exists with name: {DepartmentName}", departmentName);
            var exists = await _db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentName == departmentName, ct);
            _logger.LogInformation("Department exists with name {DepartmentName}: {Exists}", departmentName, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking department existence with name: {DepartmentName}", departmentName);
            throw;
        }
    }
}
