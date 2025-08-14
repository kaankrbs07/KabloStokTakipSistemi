using System.ComponentModel.DataAnnotations;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Manuel tetiklemeler sadece Admin
public sealed class MailController : ControllerBase
{
    private readonly IAlertService _alerts;
    private readonly ILogger<MailController> _logger;

    public MailController(IAlertService alerts, ILogger<MailController> logger)
    {
        _alerts = alerts;
        _logger = logger;
    }

    // ========== MODELS ==========
    public sealed record LowStockNotifyRequest(
        [Required] string Color,
        [Range(0, int.MaxValue)] int Qty
    );

    // ========== ENDPOINTS ==========

    /// <summary>
    /// Var olan bir Alert için (AlertID) adminlere e-posta gönderir.
    /// Alert yoksa veya admin e-postası bulunamazsa 404 döner.
    /// </summary>
    [HttpPost("alerts/{alertId:int}/notify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyAdminsForAlert([FromRoute] int alertId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("NotifyAdminsForAlert invoked for AlertID={AlertId}", alertId);
            var sent = await _alerts.NotifyAdminsForAlertAsync(alertId, ct); // BCC kullanır
            if (!sent)
            {
                _logger.LogWarning("NotifyAdminsForAlert failed (Alert not found or no admin emails). AlertID={AlertId}", alertId);
                return NotFound(new { message = "Alert bulunamadı veya gönderilecek admin e-postası yok." });
            }

            return Ok(new { message = "Yöneticilere e-posta gönderildi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotifyAdminsForAlert error. AlertID={AlertId}", alertId);
            throw; // GlobalExceptionMiddleware yakalar
        }
    }

    /// <summary>
    /// Kullanıcının belirlediği minimum seviyenin altına düşen stok için (renk-bazlı) adminlere e-posta gönderir.
    /// Trigger/SP sonrasında veya batch kontrolünde çağrılabilir.
    /// </summary>
    [HttpPost("low-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyAdminsForLowStock([FromBody] LowStockNotifyRequest req, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("NotifyAdminsForLowStock invoked for Color={Color}, Qty={Qty}", req.Color, req.Qty);
            var sent = await _alerts.NotifyAdminsForLowStockAsync(req.Color, req.Qty, ct); // BCC kullanır
            if (!sent)
            {
                _logger.LogWarning("NotifyAdminsForLowStock failed (no admin emails). Color={Color}, Qty={Qty}", req.Color, req.Qty);
                return NotFound(new { message = "Gönderilecek admin e-postası bulunamadı." });
            }

            return Ok(new { message = "Yöneticilere kritik stok uyarısı gönderildi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotifyAdminsForLowStock error. Color={Color}, Qty={Qty}", req.Color, req.Qty);
            throw; // GlobalExceptionMiddleware yakalar
        }
    }
}
