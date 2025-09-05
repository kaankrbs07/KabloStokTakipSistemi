using System.ComponentModel.DataAnnotations;
using KabloStokTakipSistemi.Middlewares; // ErrorBody, AppErrors
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

    public MailController(IAlertService alerts)
    {
        _alerts = alerts;
    }

    // ========== MODELS ==========
    public sealed record LowStockNotifyRequest(
        [Required] string Color,
        [Range(0, int.MaxValue)] int Qty
    );

    // ========== ENDPOINTS ==========


    // Var olan bir Alert için (AlertID) adminlere e-posta gönderir.
    // Alert yoksa veya admin e-postası bulunamazsa 404 döner.

    [HttpPost("alerts/{alertId:int}/notify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyAdminsForAlert([FromRoute] int alertId, CancellationToken ct)
    {
        var sent = await _alerts.NotifyAdminsForAlertAsync(alertId, ct);
        return sent ? Ok() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }


    // Renk-bazlı kritik stok için adminlere e-posta gönderir.
    // Admin e-postası bulunamazsa 404 döner.
    [HttpPost("low-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotifyAdminsForLowStock([FromBody] LowStockNotifyRequest req, CancellationToken ct)
    {
        // ModelState invalid ise [ApiController] otomatik 400 (KSTS-0400) döner.
        var sent = await _alerts.NotifyAdminsForLowStockAsync(req.Color, req.Qty, ct);
        return sent ? Ok() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }
}