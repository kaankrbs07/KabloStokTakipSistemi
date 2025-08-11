using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
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

    /// <summary>Tüm adminleri listeler</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAdmins()
    {
        var admins = await _adminService.GetAllAdminsAsync();
        return Ok(admins);
    }

    /// <summary>Belirli bir admini getirir</summary>
    [HttpGet("{adminId:long}")]
    public async Task<IActionResult> GetAdminById(long adminId)
    {
        var admin = await _adminService.GetAdminByIdAsync(adminId);
        return admin is null ? NotFound() : Ok(admin);
    }

    /// <summary>Yeni admin oluşturur (Users + Admins)</summary>
    [HttpPost]
    public async Task<IActionResult> CreateAdmin([FromBody] (CreateUserDto user, CreateAdminDto admin) dto)
    {
        var ok = await _adminService.CreateAdminAsync(dto.user, dto.admin);
        return ok ? Ok("Admin başarıyla oluşturuldu.") : BadRequest("Admin oluşturulamadı.");
    }

    /// <summary>Admin’in DepartmentName alanını günceller</summary>
    [HttpPut("{adminId:long}/department")]
    public async Task<IActionResult> UpdateDepartment(long adminId, [FromQuery] string newDepartmentName)
    {
        if (string.IsNullOrWhiteSpace(newDepartmentName))
            return BadRequest("DepartmentName boş olamaz.");

        var ok = await _adminService.UpdateAdminDepartmentAsync(adminId, newDepartmentName);
        return ok ? Ok("Admin birimi güncellendi.") : NotFound("Admin bulunamadı veya güncellenemedi.");
    }
}