﻿// Services/DepartmentService.cs
using AutoMapper;
using AutoMapper.QueryableExtensions;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Middlewares; // AppException/AppErrors

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public DepartmentService(AppDbContext db, IMapper mapper, ILogger<DepartmentService> _)
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
        long? adminId = null,
        string? search = null,
        bool? isActive = null,
        int skip = 0,
        int take = 100,
        CancellationToken ct = default)
    {
        IQueryable<Department> q = _db.Departments.AsNoTracking();

        if (adminId is not null)
            q = q.Where(d => d.AdminID == adminId);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(d => d.DepartmentName != null && d.DepartmentName.Contains(search));

        if (isActive is not null)
            q = q.Where(d => d.IsActive == isActive);

        q = q.OrderBy(d => d.DepartmentName)
             .ThenBy(d => d.DepartmentID)
             .Skip(skip).Take(take);

        return await q.ProjectTo<GetDepartmentDto>(_mapper.ConfigurationProvider).ToListAsync(ct);
    }

    public async Task<int> CreateAsync(CreateDepartmentDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.DepartmentName))
            throw new AppException(AppErrors.Validation.BadRequest, "DepartmentName boş olamaz.");

        var name = dto.DepartmentName.Trim();

        var exists = await _db.Departments.AnyAsync(d => d.DepartmentName == name, ct);
        if (exists)
            throw new AppException(AppErrors.Common.Conflict, "Bu departman adı zaten mevcut.");

        var entity = new Department
        {
            DepartmentName = name,
            AdminID = dto.AdminID,   // long? nullable
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Departments.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.DepartmentID;
    }

    public async Task<bool> UpdateAsync(int departmentId, UpdateDepartmentDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentID == departmentId, ct);
        if (entity is null) return false;

        if (string.IsNullOrWhiteSpace(dto.DepartmentName))
            throw new AppException(AppErrors.Validation.BadRequest, "DepartmentName boş olamaz.");

        var name = dto.DepartmentName.Trim();

        var clash = await _db.Departments
            .AnyAsync(d => d.DepartmentID != departmentId && d.DepartmentName == name, ct);
        if (clash)
            throw new AppException(AppErrors.Common.Conflict, "Bu departman adı zaten mevcut.");

        entity.DepartmentName = name;
        entity.AdminID = dto.AdminID;  // long? nullable

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeactivateAsync(int departmentId, CancellationToken ct = default)
    {
        var entity = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentID == departmentId, ct);
        if (entity is null) return false;

        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsByNameAsync(string departmentName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
            throw new AppException(AppErrors.Validation.BadRequest, "DepartmentName boş olamaz.");

        var name = departmentName.Trim();
        return await _db.Departments.AsNoTracking().AnyAsync(d => d.DepartmentName == name, ct);
    }
}
