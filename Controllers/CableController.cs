using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.DTOs.Cables;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Tüm metodlar için yetkilendirme, role göre özelleştirme yapılacak
public class CableController : ControllerBase
{
    private readonly ICableService _cableService;
    private readonly ILogger<CableController> _logger;

    public CableController(ICableService cableService, ILogger<CableController> logger)
    {
        _cableService = cableService;
        _logger = logger;
    }

    // ===================== SINGLE CABLE =====================

    [HttpGet("single")]
    public async Task<IActionResult> GetAllSingleCables()
    {
        var cables = await _cableService.GetAllSingleCablesAsync();
        return Ok(cables);
    }

    [HttpGet("single/{id}")]
    public async Task<IActionResult> GetSingleCableById(int id)
    {
        var cable = await _cableService.GetSingleCableByIdAsync(id);
        return cable is null ? NotFound() : Ok(cable);
    }

    [HttpPost("single")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSingleCable([FromBody] CreateSingleCableDto dto)
    {
        var success = await _cableService.CreateSingleCableAsync(dto);
        return success ? Ok() : BadRequest();
    }

    [HttpDelete("single/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateSingleCable(int id)
    {
        var success = await _cableService.DeactivateSingleCableAsync(id);
        return success ? Ok() : NotFound();
    }

    [HttpGet("single/inactive")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetInactiveSingleCables()
    {
        var cables = await _cableService.GetInactiveSingleCablesAsync();
        return Ok(cables);
    }

    // ===================== MULTI CABLE =====================

    [HttpGet("multi")]
    public async Task<IActionResult> GetAllMultiCables()
    {
        var cables = await _cableService.GetAllMultiCablesAsync();
        return Ok(cables);
    }

    [HttpGet("multi/{id}")]
    public async Task<IActionResult> GetMultiCableById(int id)
    {
        var cable = await _cableService.GetMultiCableByIdAsync(id);
        return cable is null ? NotFound() : Ok(cable);
    }

    [HttpPost("multi")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateMultiCable([FromBody] CreateMultiCableDto dto)
    {
        var success = await _cableService.CreateMultiCableAsync(dto);
        return success ? Ok() : BadRequest();
    }

    [HttpDelete("multi/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateMultiCable(int id)
    {
        var success = await _cableService.DeactivateMultiCableAsync(id);
        return success ? Ok() : NotFound();
    }

    [HttpGet("multi/inactive")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetInactiveMultiCables()
    {
        var cables = await _cableService.GetInactiveMultiCablesAsync();
        return Ok(cables);
    }

    // ===================== MULTI CONTENT =====================

    [HttpGet("multi/{id}/contents")]
    public async Task<IActionResult> GetMultiCableContents(int id)
    {
        var contents = await _cableService.GetMultiCableContentsAsync(id);
        return Ok(contents);
    }

    // ===================== STOCK MOVEMENTS =====================

    [HttpPost("stock-movement")]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> InsertStockMovement([FromBody] CreateStockMovementDto dto)
    {
        var success = await _cableService.InsertStockMovementAsync(
            dto.CableID, dto.TableName, dto.Quantity, dto.MovementType, dto.UserID
        );
        return success ? Ok() : BadRequest();
    }

    // ===================== THRESHOLDS =====================

    [HttpGet("thresholds/color")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetColorThresholds()
    {
        var result = await _cableService.GetColorThresholdsAsync();
        return Ok(result);
    }

    [HttpPost("thresholds/color")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetColorThreshold([FromBody] CreateColorThresholdDto dto)
    {
        var success = await _cableService.SetColorThresholdAsync(dto);
        return success ? Ok() : BadRequest();
    }

    [HttpGet("thresholds/cable")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCableThresholds()
    {
        var result = await _cableService.GetCableThresholdsAsync();
        return Ok(result);
    }

    [HttpPost("thresholds/cable")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetCableThreshold([FromBody] CreateCableThresholdDto dto)
    {
        var success = await _cableService.SetCableThresholdAsync(dto);
        return success ? Ok() : BadRequest();
    }
}