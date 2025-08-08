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

    /// <summary>
    /// Tüm adminleri listeler
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAdmins()
    {
        var admins = await _adminService.GetAllAdminsAsync();
        return Ok(admins);
    }

    /// <summary>
    /// Belirli bir admini getirir
    /// </summary>
    [HttpGet("{adminId:long}")]
    public async Task<IActionResult> GetAdminById(long adminId)
    {
        var admin = await _adminService.GetAdminByIdAsync(adminId);
        if (admin == null)
            return NotFound();

        return Ok(admin);
    }

    /// <summary>
    /// Yeni admin oluşturur (User + Admin)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAdmin([FromBody] (CreateUserDto user, CreateAdminDto admin) dto)
    {
        var success = await _adminService.CreateAdminAsync(dto.user, dto.admin);
        if (!success)
            return BadRequest("Admin oluşturulamadı.");

        return Ok("Admin başarıyla oluşturuldu.");
    }

    /// <summary>
    /// Adminin bağlı olduğu birimi günceller
    /// </summary>
    [HttpPut("{adminId:long}/department")]
    public async Task<IActionResult> UpdateDepartment(long adminId, [FromQuery] int newDepartmentId)
    {
        var success = await _adminService.UpdateAdminDepartmentAsync(adminId, newDepartmentId);
        if (!success)
            return NotFound("Admin bulunamadı veya güncellenemedi.");

        return Ok("Admin birimi güncellendi.");
    }
}