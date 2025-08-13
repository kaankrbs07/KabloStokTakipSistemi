using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Sadece adminler erişebilir
public class LogsController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly ILogger<LogsController> _logger;
    private static readonly NLog.Logger _nlogger = LogManager.GetCurrentClassLogger();

    public LogsController(ILogService logService, ILogger<LogsController> logger)
    {
        _logService = logService;
        _logger = logger;
    }


    /// Filtreye göre log listesini döner (sayfalama destekli).
    [HttpPost("filter")]
    public async Task<IActionResult> GetLogs([FromBody] LogFilterDto filter, CancellationToken ct)
    {
        try
        {
            var result = await _logService.GetAsync(filter, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting filtered logs");
            _nlogger.Error(ex, "Error while getting filtered logs");
            return StatusCode(500, "Log kayıtları getirilirken hata oluştu.");
        }
    }


    /// En son log kayıtlarını döner.
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromQuery] int take = 50, CancellationToken ct = default)
    {
        try
        {
            var result = await _logService.GetLatestAsync(take, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting latest logs");
            _nlogger.Error(ex, "Error while getting latest logs");
            return StatusCode(500, "Son log kayıtları getirilirken hata oluştu.");
        }
    }


    /// Operasyon türüne göre log sayıları.
    [HttpGet("stats/operations")]
    public async Task<IActionResult> GetCountByOperation([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _logService.GetCountByOperationAsync(from, to, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting log statistics by operation");
            _nlogger.Error(ex, "Error while getting log statistics by operation");
            return StatusCode(500, "Operasyon bazlı log istatistikleri getirilirken hata oluştu.");
        }
    }


    /// Tablo adına göre log sayıları.
    [HttpGet("stats/tables")]
    public async Task<IActionResult> GetCountByTable([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _logService.GetCountByTableAsync(from, to, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting log statistics by table");
            _nlogger.Error(ex, "Error while getting log statistics by table");
            return StatusCode(500, "Tablo bazlı log istatistikleri getirilirken hata oluştu.");
        }
    }
    
    /// Manuel stok düzeltme işlemini loglar.
    [HttpPost("manual-stock-edit")]
    public async Task<IActionResult> LogManualStockEdit([FromBody] ManualStockEditLogDto dto,
        CancellationToken ct = default)
    {
        try
        {
            var success = await _logService.LogManualStockEditAsync(dto, ct);
            return success
                ? Ok(new { message = "Manual stock edit logged successfully." })
                : BadRequest(new { message = "Manual stock edit logging failed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while logging manual stock edit");
            _nlogger.Error(ex, "Error while logging manual stock edit");
            return StatusCode(500, "Manuel stok düzeltme loglanırken hata oluştu.");
        }
    }
}