using KabloStokTakipSistemi.DTOs;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IDepartmentService
{
    Task<GetDepartmentDto?> GetByIdAsync(int departmentId, CancellationToken ct = default);

    Task<IReadOnlyList<GetDepartmentDto>> GetAsync(
        long? adminId = null,          // int? -> long?
        string? search = null,
        bool? isActive = null,
        int skip = 0,
        int take = 100,
        CancellationToken ct = default);

    Task<int> CreateAsync(CreateDepartmentDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(int departmentId, UpdateDepartmentDto dto, CancellationToken ct = default);
    Task<bool> DeactivateAsync(int departmentId, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string departmentName, CancellationToken ct = default);
}