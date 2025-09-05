using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.DTOs.Cables;
using KabloStokTakipSistemi.Services.Interfaces;
using KabloStokTakipSistemi.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Tüm metodlar için yetkilendirme, role bazlı özelleştirme altta
public sealed class CableController : ControllerBase
{
    private readonly ICableService _cableService;
    public CableController(ICableService cableService) => _cableService = cableService;

    // ===================== SINGLE CABLE =====================

    [HttpGet("single")]
    public async Task<IActionResult> GetAllSingleCables()
    {
        var cables = await _cableService.GetAllSingleCablesAsync();
        return Ok(cables);
    }

    [HttpGet("single/{id:int}")]
    public async Task<IActionResult> GetSingleCableById(int id)
    {
        var cable = await _cableService.GetSingleCableByIdAsync(id);
        return cable is null
            ? NotFound(new ErrorBody(AppErrors.Common.NotFound.Code))
            : Ok(cable);
    }

    [HttpPost("single")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSingleCable([FromBody] CreateSingleCableDto dto)
    {
        var ok = await _cableService.CreateSingleCableAsync(dto);
        return ok ? Ok() : BadRequest(new ErrorBody(AppErrors.Common.Unexpected.Code));
    }

    [HttpPatch("single/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateSingleCable(int id)
    {
        var ok = await _cableService.DeactivateSingleCableAsync(id);
        return ok ? NoContent() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
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

    [HttpGet("multi/{id:int}")]
    public async Task<IActionResult> GetMultiCableById(int id)
    {
        var cable = await _cableService.GetMultiCableByIdAsync(id);
        return cable is null
            ? NotFound(new ErrorBody(AppErrors.Common.NotFound.Code))
            : Ok(cable);
    }

    [HttpPost("multi")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateMultiCable([FromBody] CreateMultiCableDto dto)
    {
        var ok = await _cableService.CreateMultiCableAsync(dto);
        return ok ? Ok() : BadRequest(new ErrorBody(AppErrors.Common.Unexpected.Code));
    }

    [HttpPatch("multi/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateMultiCable(int id)
    {
        var ok = await _cableService.DeactivateMultiCableAsync(id);
        return ok ? NoContent() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }

    [HttpGet("multi/inactive")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetInactiveMultiCables()
    {
        var cables = await _cableService.GetInactiveMultiCablesAsync();
        return Ok(cables);
    }

    // ===================== MULTI CONTENT =====================

    [HttpGet("multi/{id:int}/contents")]
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
        var ok = await _cableService.InsertStockMovementAsync(dto); // Doğrudan DTO'yu geçir
        return ok ? Ok() : BadRequest(new ErrorBody(AppErrors.Validation.BadRequest.Code));
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
        var ok = await _cableService.SetColorThresholdAsync(dto);
        return ok ? Ok() : BadRequest(new ErrorBody(AppErrors.Validation.BadRequest.Code));
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
        var ok = await _cableService.SetCableThresholdAsync(dto);
        return ok ? Ok() : BadRequest(new ErrorBody(AppErrors.Validation.BadRequest.Code));
    }
}
