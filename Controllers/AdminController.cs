using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>Tüm adminleri listeler</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAdmins()
    {
        try
        {
            _logger.LogInformation("Getting all admins");
            var admins = await _adminService.GetAllAdminsAsync();
            _logger.LogInformation("Retrieved {Count} admins", admins.Count());
            return Ok(admins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all admins");
            throw;
        }
    }

    /// <summary>Belirli bir admini getirir</summary>
    [HttpGet("{adminId:long}")]
    public async Task<IActionResult> GetAdminById(long adminId)
    {
        try
        {
            _logger.LogInformation("Getting admin with ID: {AdminId}", adminId);
            var admin = await _adminService.GetAdminByIdAsync(adminId);
            
            if (admin is null)
            {
                _logger.LogWarning("Admin not found with ID: {AdminId}", adminId);
                return NotFound();
            }
            
            _logger.LogInformation("Retrieved admin with ID: {AdminId}", adminId);
            return Ok(admin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin with ID: {AdminId}", adminId);
            throw;
        }
    }

    /// <summary>Yeni admin oluşturur (Users + Admins)</summary>
    [HttpPost]
    public async Task<IActionResult> CreateAdmin([FromBody] (CreateUserDto user, CreateAdminDto admin) dto)
    {
        try
        {
            _logger.LogInformation("Creating new admin with user ID: {UserId}", dto.user.UserID);
            var ok = await _adminService.CreateAdminAsync(dto.user, dto.admin);
            
            if (!ok)
            {
                _logger.LogWarning("Failed to create admin with user ID: {UserId}", dto.user.UserID);
                return BadRequest("Admin oluşturulamadı.");
            }
            
            _logger.LogInformation("Successfully created admin with user ID: {UserId}", dto.user.UserID);
            return Ok("Admin başarıyla oluşturuldu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin with user ID: {UserId}", dto.user.UserID);
            throw;
        }
    }

    /// <summary>Admin'in DepartmentName alanını günceller</summary>
    [HttpPut("{adminId:long}/department")]
    public async Task<IActionResult> UpdateDepartment(long adminId, [FromQuery] string newDepartmentName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newDepartmentName))
            {
                _logger.LogWarning("Empty department name provided for admin ID: {AdminId}", adminId);
                return BadRequest("DepartmentName boş olamaz.");
            }

            _logger.LogInformation("Updating department for admin ID: {AdminId} to: {DepartmentName}", adminId, newDepartmentName);
            var ok = await _adminService.UpdateAdminDepartmentAsync(adminId, newDepartmentName);
            
            if (!ok)
            {
                _logger.LogWarning("Failed to update department for admin ID: {AdminId}", adminId);
                return NotFound("Admin bulunamadı veya güncellenemedi.");
            }
            
            _logger.LogInformation("Successfully updated department for admin ID: {AdminId}", adminId);
            return Ok("Admin birimi güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department for admin ID: {AdminId}", adminId);
            throw;
        }
    }
}