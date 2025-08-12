
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
    private readonly ILogger<AlertController> _logger;

    public AlertController(IAlertService alerts, ILogger<AlertController> logger)
    {
        _alerts = alerts;
        _logger = logger;
    }

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
        try
        {
            _logger.LogInformation("Getting alerts with filters - IsActive: {IsActive}, AlertType: {AlertType}, Color: {Color}, MultiCableId: {MultiCableId}", 
                isActive, alertType, color, multiCableId);
            var list = await _alerts.GetAlertsAsync(isActive, alertType, color, multiCableId, from, to, skip, take, ct);
            _logger.LogInformation("Retrieved {Count} alerts", list.Count);
            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts with filters");
            throw;
        }
    }

    /// <summary>AlertID'ye göre tek uyarıyı getirir.</summary>
    [HttpGet("{alertId:int}")]
    [ProducesResponseType(typeof(GetAlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int alertId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting alert with ID: {AlertId}", alertId);
            var dto = await _alerts.GetByIdAsync(alertId, ct);
            
            if (dto is null)
            {
                _logger.LogWarning("Alert not found with ID: {AlertId}", alertId);
                return NotFound();
            }
            
            _logger.LogInformation("Retrieved alert with ID: {AlertId}", alertId);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert with ID: {AlertId}", alertId);
            throw;
        }
    }

    /// <summary>Belirli bir renk için aktif uyarı var mı?</summary>
    [HttpGet("has-active-for-color")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> HasActiveForColor([FromQuery][Required] string color, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Checking if there's an active alert for color: {Color}", color);
            var has = await _alerts.HasActiveAlertForColorAsync(color, ct);
            _logger.LogInformation("Active alert for color {Color}: {HasActive}", color, has);
            return Ok(new { color, hasActive = has });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking active alert for color: {Color}", color);
            throw;
        }
    }

    // ---- STATE CHANGES ----

    /// <summary>Uyarıyı kapatır (resolve).</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{alertId:int}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve([FromRoute] int alertId, [FromBody] ResolveRequest? body, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Resolving alert with ID: {AlertId}", alertId);
            var ok = await _alerts.ResolveAsync(alertId, body?.Note, ct);
            
            if (!ok)
            {
                _logger.LogWarning("Failed to resolve alert with ID: {AlertId}", alertId);
                return NotFound();
            }
            
            _logger.LogInformation("Successfully resolved alert with ID: {AlertId}", alertId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert with ID: {AlertId}", alertId);
            throw;
        }
    }

    /// <summary>Uyarıyı yeniden aktif eder.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{alertId:int}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate([FromRoute] int alertId, [FromBody] ReactivateRequest? body, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Reactivating alert with ID: {AlertId}", alertId);
            var ok = await _alerts.ReactivateAsync(alertId, body?.Reason, ct);
            
            if (!ok)
            {
                _logger.LogWarning("Failed to reactivate alert with ID: {AlertId}", alertId);
                return NotFound();
            }
            
            _logger.LogInformation("Successfully reactivated alert with ID: {AlertId}", alertId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating alert with ID: {AlertId}", alertId);
            throw;
        }
    }

    // ---- EMAIL NOTIFICATIONS ----

    /// <summary>Mevcut bir uyarı için adminlere e-posta yollar.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{alertId:int}/notify-admins")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyAdmins([FromRoute] int alertId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Sending admin notification for alert ID: {AlertId}", alertId);
            var ok = await _alerts.NotifyAdminsForAlertAsync(alertId, ct);
            
            if (!ok)
            {
                _logger.LogWarning("Failed to send admin notification for alert ID: {AlertId}", alertId);
                return NotFound(new { message = "Alert bulunamadı veya admin e-postası yok." });
            }
            
            _logger.LogInformation("Successfully sent admin notification for alert ID: {AlertId}", alertId);
            return Ok(new { message = "Adminlere e-posta gönderildi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending admin notification for alert ID: {AlertId}", alertId);
            throw;
        }
    }

    /// <summary>Renk bazlı kritik stok uyarısını adminlere e-posta olarak yollar.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("notify-low-stock")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyLowStock([FromBody] LowStockNotifyRequest body, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for low stock notification request");
                return ValidationProblem(ModelState);
            }
            
            _logger.LogInformation("Sending low stock notification for color: {Color}, quantity: {Qty}", body.Color, body.Qty);
            var ok = await _alerts.NotifyAdminsForLowStockAsync(body.Color, body.Qty, ct);
            
            if (!ok)
            {
                _logger.LogWarning("Failed to send low stock notification for color: {Color}", body.Color);
                return NotFound(new { message = "Admin e-postası bulunamadı." });
            }
            
            _logger.LogInformation("Successfully sent low stock notification for color: {Color}", body.Color);
            return Ok(new { message = $"{body.Color} rengi için stok uyarısı gönderildi.", body.Qty });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending low stock notification for color: {Color}", body.Color);
            throw;
        }
    }

    // ---- Request bodies ----
    public sealed record ResolveRequest(string? Note);
    public sealed record ReactivateRequest(string? Reason);
    public sealed record LowStockNotifyRequest([property: Required] string Color,
                                               [property: Range(0, int.MaxValue)] int Qty);
}
