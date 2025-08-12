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

    public DepartmentService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<GetDepartmentDto?> GetByIdAsync(int departmentId, CancellationToken ct = default)
    {
        return await _db.Departments.AsNoTracking()
            .Where(d => d.DepartmentID == departmentId)
            .ProjectTo<GetDepartmentDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<GetDepartmentDto>> GetAsync(
        int? adminId = null,
        string? search = null,
        int skip = 0,
        int take = 100,
        CancellationToken ct = default)
    {
        IQueryable<Department> q = _db.Departments.AsNoTracking();

        if (adminId is not null)
            q = q.Where(d => d.AdminID == adminId);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(d => d.DepartmentName != null && d.DepartmentName.Contains(search));

        q = q.OrderBy(d => d.DepartmentName).ThenBy(d => d.DepartmentID)
             .Skip(skip).Take(take);

        return await q.ProjectTo<GetDepartmentDto>(_mapper.ConfigurationProvider).ToListAsync(ct);
    }

    public async Task<int> CreateAsync(CreateDepartmentDto dto, CancellationToken ct = default)
    {
        // Temel doğrulama (DB’de NULL olsa bile uygulama seviyesinde boş bırakmıyoruz)
        if (string.IsNullOrWhiteSpace(dto.DepartmentName))
            throw new ArgumentException("DepartmentName boş olamaz.");
        
        var exists = await _db.Departments.AnyAsync(d => d.DepartmentName == dto.DepartmentName, ct);
        if (exists) throw new InvalidOperationException("Bu departman adı zaten mevcut.");

        var entity = new Department
        {
            DepartmentName = dto.DepartmentName.Trim(),
            AdminID = dto.AdminID,
            CreatedAt = DateTime.Now
        };

        _db.Departments.Add(entity);

        // INSERT tetikleyicisi (trg_Log_InsertDepartment) log’u DB tarafında yazacak.
        await _db.SaveChangesAsync(ct);
        return entity.DepartmentID;
    }

    public async Task<bool> UpdateAsync(int departmentId, UpdateDepartmentDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentID == departmentId, ct);
        if (entity is null) return false;

        if (string.IsNullOrWhiteSpace(dto.DepartmentName))
            throw new ArgumentException("DepartmentName boş olamaz.");

        // (Opsiyonel) Aynı isim kontrolü — kendisi hariç
        var nameClash = await _db.Departments
            .AnyAsync(d => d.DepartmentID != departmentId && d.DepartmentName == dto.DepartmentName, ct);
        if (nameClash) throw new InvalidOperationException("Bu departman adı zaten mevcut.");

        entity.DepartmentName = dto.DepartmentName.Trim();
        entity.AdminID = dto.AdminID;

        // Not: UPDATE için ayrıca trigger yok; Log gerekiyorsa DB tarafına eklenebilir.
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int departmentId, CancellationToken ct = default)
    {
        var entity = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentID == departmentId, ct);
        if (entity is null) return false;

        _db.Departments.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsByNameAsync(string departmentName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(departmentName)) return false;
        return await _db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentName == departmentName, ct);
    }
}
