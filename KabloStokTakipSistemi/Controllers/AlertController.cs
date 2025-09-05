using System.ComponentModel.DataAnnotations;
using KabloStokTakipSistemi.Services.Interfaces;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AlertController : ControllerBase
{
    private readonly IAlertService _alerts;
    public AlertController(IAlertService alerts) => _alerts = alerts;

    // ---- QUERY ----

    // Uyarıları filtreleyerek listeler 
    
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] bool? isActive,
        [FromQuery] string? alertType,
        [FromQuery] string? color,
        [FromQuery] int? multiCableId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? skip,
        [FromQuery] int? take,
        CancellationToken ct)
    {
        var list = await _alerts.GetAlertsAsync(isActive, alertType, color, multiCableId, from, to, skip, take, ct);
        return Ok(list);
    }

    // AlertID'ye göre tek uyarı 
    
    [HttpGet("{alertId:int}")]
    public async Task<IActionResult> GetById([FromRoute] int alertId, CancellationToken ct)
    {
        var dto = await _alerts.GetByIdAsync(alertId, ct);
        return dto is null
            ? NotFound(new ErrorBody(AppErrors.Common.NotFound.Code))
            : Ok(dto);
    }

    // Belirli bir renk için aktif uyarı var mı? 
    
    [HttpGet("has-active-for-color")]
    public async Task<IActionResult> HasActiveForColor([FromQuery][Required] string color, CancellationToken ct)
    {
        // Model attribute'u invalid ise ApiBehaviorOptions 400 döndürür (ErrorBody: KSTS-0400)
        var has = await _alerts.HasActiveAlertForColorAsync(color, ct);
        return Ok(new { color, hasActive = has });
    }

    // ---- STATE CHANGES ----

    [Authorize(Roles = "Admin")]
    [HttpPost("{alertId:int}/resolve")]
    public async Task<IActionResult> Resolve([FromRoute] int alertId, [FromBody] ResolveRequest? body, CancellationToken ct)
    {
        var ok = await _alerts.ResolveAsync(alertId, body?.Note, ct);
        return ok ? NoContent() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{alertId:int}/reactivate")]
    public async Task<IActionResult> Reactivate([FromRoute] int alertId, [FromBody] ReactivateRequest? body, CancellationToken ct)
    {
        var ok = await _alerts.ReactivateAsync(alertId, body?.Reason, ct);
        return ok ? NoContent() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }

    // ---- EMAIL NOTIFICATIONS ----

    [Authorize(Roles = "Admin")]
    [HttpPost("{alertId:int}/notify-admins")]
    public async Task<IActionResult> NotifyAdmins([FromRoute] int alertId, CancellationToken ct)
    {
        var ok = await _alerts.NotifyAdminsForAlertAsync(alertId, ct);
        return ok ? Ok() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("notify-low-stock")]
    public async Task<IActionResult> NotifyLowStock([FromBody] LowStockNotifyRequest body, CancellationToken ct)
    {
        // ModelState invalid ise otomatik 400 (ErrorBody: KSTS-0400)
        var ok = await _alerts.NotifyAdminsForLowStockAsync(body.Color, body.Qty, ct);
        return ok ? Ok() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }

    // ---- Request bodies ----
    public sealed record ResolveRequest(string? Note);
    public sealed record ReactivateRequest(string? Reason);
    public sealed record LowStockNotifyRequest(
        [property: Required] string Color,
        [property: Range(0, int.MaxValue)] int Qty);
}
