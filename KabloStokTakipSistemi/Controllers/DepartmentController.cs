
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Services.Interfaces;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/department")]
public sealed class DepartmentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDepartmentService _departmentService;

    public DepartmentController(AppDbContext db, IDepartmentService departmentService)
    {
        _db = db;
        _departmentService = departmentService;
    }

    // ---- PUBLIC: SignUp ekranı burayı çağıracak ----
    [HttpGet("public/names")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicNames(CancellationToken ct)
    {
        var names = await _db.Departments.AsNoTracking()
                         .OrderBy(d => d.DepartmentName)
                         .Select(d => d.DepartmentName!)
                         .ToListAsync(ct);

        return Ok(names);
    }

    [HttpGet("has-admin")]
    [AllowAnonymous]
    public async Task<IActionResult> HasAdmin([FromQuery] string name, CancellationToken ct)
    {
        var deptName = name?.Trim();
        if (string.IsNullOrWhiteSpace(deptName))
            return BadRequest(new { hasAdmin = false, message = "Departman adı zorunlu." });

        var has = await _db.Admins.AsNoTracking()
            .AnyAsync(a => a.DepartmentName == deptName, ct);

        return Ok(new { hasAdmin = has });
    }

    [HttpGet("public/resolve-id")]
    [AllowAnonymous]
    public async Task<IActionResult> ResolveId([FromQuery] string name, CancellationToken ct)
    {
        var id = await _db.Departments.AsNoTracking()
            .Where(d => d.DepartmentName == name)
            .Select(d => (int?)d.DepartmentID)
            .FirstOrDefaultAsync(ct);
        return Ok(new { departmentId = id });
    }

    // ---- ADMIN aksiyonları ----
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDepartments(
        [FromQuery] long? adminId,   // int? → long?
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
        => Ok(await _departmentService.GetAsync(adminId, search, isActive, skip, take, ct));

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDepartmentById(int id, CancellationToken ct)
    {
        var d = await _departmentService.GetByIdAsync(id, ct);
        return d is null ? NotFound() : Ok(d);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto dto, CancellationToken ct)
    {
        var newId = await _departmentService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetDepartmentById), new { id = newId }, dto);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto dto, CancellationToken ct)
        => (await _departmentService.UpdateAsync(id, dto, ct)) ? NoContent() : NotFound();

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateDepartment(int id, CancellationToken ct)
        => (await _departmentService.DeactivateAsync(id, ct)) ? NoContent() : NotFound();

    [HttpGet("exists")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DepartmentExists([FromQuery] string name, CancellationToken ct)
        => Ok(new { exists = await _departmentService.ExistsByNameAsync(name, ct) });
}
