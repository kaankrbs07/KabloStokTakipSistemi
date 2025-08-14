using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using KabloStokTakipSistemi.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    // Tüm adminleri listeler
    [HttpGet]
    public async Task<IActionResult> GetAllAdmins()
    {
        var admins = await _adminService.GetAllAdminsAsync();
        return Ok(admins);
    }

    // Belirli bir admini getirir
    [HttpGet("{adminId:long}")]
    public async Task<IActionResult> GetAdminById(long adminId)
    {
        var admin = await _adminService.GetAdminByIdAsync(adminId);
        return admin is null
            ? NotFound(new ErrorBody(AppErrors.Common.NotFound.Code))
            : Ok(admin);
    }

    // Yeni admin oluşturur
    [HttpPost]
    public async Task<IActionResult> CreateAdmin([FromBody] (CreateUserDto user, CreateAdminDto admin) dto)
    {
        var ok = await _adminService.CreateAdminAsync(dto.user, dto.admin);
        return ok ? Ok() : BadRequest(new ErrorBody(AppErrors.Common.Unexpected.Code));
    }

    // Admin'in DepartmentName alanını günceller
    [HttpPatch("{adminId:long}/department")]
    public async Task<IActionResult> UpdateDepartment(long adminId, [FromQuery] string newDepartmentName)
    {
        if (string.IsNullOrWhiteSpace(newDepartmentName))
            return BadRequest(new ErrorBody(AppErrors.Validation.BadRequest.Code));

        var ok = await _adminService.UpdateAdminDepartmentAsync(adminId, newDepartmentName);
        return ok ? NoContent() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }
}