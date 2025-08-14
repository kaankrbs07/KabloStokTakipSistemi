using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Departman yönetimi sadece adminlere açık
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(IDepartmentService departmentService, ILogger<DepartmentController> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    // Departmanları listele (filtreli)
    [HttpGet]
    public async Task<IActionResult> GetDepartments(
        [FromQuery] int? adminId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        var result = await _departmentService.GetAsync(adminId, search, isActive, skip, take);
        return Ok(result);
    }

    // ID'ye göre departman getir
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDepartmentById(int id)
    {
        var department = await _departmentService.GetByIdAsync(id);
        return department is null ? NotFound() : Ok(department);
    }

    // Departman ekle
    [HttpPost]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto dto)
    {
        var newId = await _departmentService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetDepartmentById), new { id = newId }, dto);
    }

    // Departman güncelle
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto dto)
    {
        var success = await _departmentService.UpdateAsync(id, dto);
        return success ? NoContent() : NotFound();
    }

    // Departmanı pasif hale getir
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> DeactivateDepartment(int id)
    {
        var success = await _departmentService.DeactivateAsync(id); // Silme yerine pasifleştirme yapacak
        return success ? NoContent() : NotFound();
    }

    // Departman ismi var mı kontrol et
    [HttpGet("exists")]
    public async Task<IActionResult> DepartmentExists([FromQuery] string name)
    {
        var exists = await _departmentService.ExistsByNameAsync(name);
        return Ok(new { exists });
    }
}