using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Middlewares;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Sadece adminler erişebilir
public sealed class LogsController : ControllerBase
{
    private readonly ILogService _logService;

    public LogsController(ILogService logService)
    {
        _logService = logService;
    }

    /// Filtreye göre log listesini döner (sayfalama destekli).
    [HttpPost("filter")]
    public async Task<IActionResult> GetLogs([FromBody] LogFilterDto filter, CancellationToken ct)
    {
        var result = await _logService.GetAsync(filter, ct);
        return Ok(result);
    }

    /// En son log kayıtlarını döner.
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromQuery] int take = 50, CancellationToken ct = default)
    {
        var result = await _logService.GetLatestAsync(take, ct);
        return Ok(result);
    }

    /// Operasyon türüne göre log sayıları.
    [HttpGet("stats/operations")]
    public async Task<IActionResult> GetCountByOperation([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        var result = await _logService.GetCountByOperationAsync(from, to, ct);
        return Ok(result);
    }

    /// Tablo adına göre log sayıları.
    [HttpGet("stats/tables")]
    public async Task<IActionResult> GetCountByTable([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        var result = await _logService.GetCountByTableAsync(from, to, ct);
        return Ok(result);
    }

    /// Manuel stok düzeltme işlemini loglar.
    [HttpPost("manual-stock-edit")]
    public async Task<IActionResult> LogManualStockEdit([FromBody] ManualStockEditLogDto dto, CancellationToken ct = default)
    {
        var success = await _logService.LogManualStockEditAsync(dto, ct);
        return success
            ? Ok()
            : BadRequest(new ErrorBody(AppErrors.Validation.BadRequest.Code));
    }
}
