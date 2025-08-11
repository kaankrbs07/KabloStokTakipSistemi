// Controllers/AlertController.cs
using System.ComponentModel.DataAnnotations;
using KabloStokTakipSistemi.Services.Interfaces;
using KabloStokTakipSistemi.DTOs;
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

    /// <summary>Uyarıları filtreleyerek listeler.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GetAlertDto>), StatusCodes.Status200OK)]
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

    /// <summary>AlertID'ye göre tek uyarıyı getirir.</summary>
    [HttpGet("{alertId:int}")]
    [ProducesResponseType(typeof(GetAlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int alertId, CancellationToken ct)
    {
        var dto = await _alerts.GetByIdAsync(alertId, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Belirli bir renk için aktif uyarı var mı?</summary>
    [HttpGet("has-active-for-color")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> HasActiveForColor([FromQuery][Required] string color, CancellationToken ct)
    {
        var has = await _alerts.HasActiveAlertForColorAsync(color, ct);
        return Ok(new { color, hasActive = has });
    }

    // ---- STATE CHANGES ----

    /// <summary>Uyarıyı kapatır (resolve).</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{alertId:int}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve([FromRoute] int alertId, [FromBody] ResolveRequest? body, CancellationToken ct)
    {
        var ok = await _alerts.ResolveAsync(alertId, body?.Note, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Uyarıyı yeniden aktif eder.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{alertId:int}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate([FromRoute] int alertId, [FromBody] ReactivateRequest? body, CancellationToken ct)
    {
        var ok = await _alerts.ReactivateAsync(alertId, body?.Reason, ct);
        return ok ? NoContent() : NotFound();
    }

    // ---- EMAIL NOTIFICATIONS ----

    /// <summary>Mevcut bir uyarı için adminlere e-posta yollar.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{alertId:int}/notify-admins")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyAdmins([FromRoute] int alertId, CancellationToken ct)
    {
        var ok = await _alerts.NotifyAdminsForAlertAsync(alertId, ct);
        return ok
            ? Ok(new { message = "Adminlere e-posta gönderildi." })
            : NotFound(new { message = "Alert bulunamadı veya admin e-postası yok." });
    }

    /// <summary>Renk bazlı kritik stok uyarısını adminlere e-posta olarak yollar.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("notify-low-stock")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyLowStock([FromBody] LowStockNotifyRequest body, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var ok = await _alerts.NotifyAdminsForLowStockAsync(body.Color, body.Qty, ct);
        return ok
            ? Ok(new { message = $"{body.Color} rengi için stok uyarısı gönderildi.", body.Qty })
            : NotFound(new { message = "Admin e-postası bulunamadı." });
    }

    // ---- Request bodies ----
    public sealed record ResolveRequest(string? Note);
    public sealed record ReactivateRequest(string? Reason);
    public sealed record LowStockNotifyRequest([property: Required] string Color,
                                               [property: Range(0, int.MaxValue)] int Qty);
}
