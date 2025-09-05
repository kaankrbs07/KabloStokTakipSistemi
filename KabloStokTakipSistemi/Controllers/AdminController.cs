using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using KabloStokTakipSistemi.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

 
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
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

    // Belirli bir admini getirir (Username ile)
    [HttpGet("{username}")]
    public async Task<IActionResult> GetAdminByUsername(string username)
    {
        var admin = await _adminService.GetAdminByUsernameAsync(username);
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
    [HttpPatch("{username}/department")]
    public async Task<IActionResult> UpdateDepartment(string username, [FromQuery] string newDepartmentName)
    {
        if (string.IsNullOrWhiteSpace(newDepartmentName))
            return BadRequest(new ErrorBody(AppErrors.Validation.BadRequest.Code));

        var ok = await _adminService.UpdateAdminDepartmentAsync(username, newDepartmentName);
        return ok ? NoContent() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }
}