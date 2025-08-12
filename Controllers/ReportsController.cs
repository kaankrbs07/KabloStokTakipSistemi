using System.Net.Mime;
using KabloStokTakipSistemi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using KabloStokTakipSistemi.Services.Interfaces;
using KabloStokTakipSistemi.DTOs.Users;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reports, ILogger<ReportsController> logger)
    {
        _reports = reports;
        _logger = logger;
    }

    /// <summary>
    /// Belirli bir kullanıcı için stok hareketleri özeti (SP: sp_GetUserActivitySummary).
    /// </summary>
    [HttpGet("users/{userId:long}/activity")]
    [ProducesResponseType(typeof(UserActivitySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserActivitySummary([FromRoute] long userId, CancellationToken ct)
    {
        try
        {
            var data = await _reports.GetUserActivitySummaryAsync(userId, ct);
            if (data is null) return NoContent(); // SP satır döndürmediyse

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetUserActivitySummary failed for UserID={UserID}", userId);
            return Problem(title: "Rapor oluşturulamadı.", detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Son 30 günde çoklu kablolardan renge göre toplam çıkış (SP: sp_GetMonthlyReport_MultiCables).
    /// </summary>
    [HttpGet("multi/monthly")]
    [ProducesResponseType(typeof(IReadOnlyList<MonthlyMultiCableReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMonthlyReportForMultiCables(CancellationToken ct)
    {
        try
        {
            var list = await _reports.GetMonthlyReportForMultiCablesAsync(ct);
            if (list is null || list.Count == 0) return NoContent();

            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMonthlyReportForMultiCables failed");
            return Problem(title: "Rapor oluşturulamadı.", detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Son 30 günde en çok kullanılan 5 tekli kablonun (renk) aylık kullanım karşılaştırması
    /// (SP: sp_GetMonthlyReport_SingleCables).
    /// </summary>
    [HttpGet("single/monthly")]
    [ProducesResponseType(typeof(IReadOnlyList<MonthlySingleCableReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMonthlyReportForSingleCables(CancellationToken ct)
    {
        try
        {
            var list = await _reports.GetMonthlyReportForSingleCablesAsync(ct);
            if (list is null || list.Count == 0) return NoContent();

            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMonthlyReportForSingleCables failed");
            return Problem(title: "Rapor oluşturulamadı.", detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}