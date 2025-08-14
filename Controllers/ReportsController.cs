using System.Net.Mime;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;
    public ReportsController(IReportService reports) => _reports = reports;

    // Belirli bir kullanıcı için stok hareketleri özeti (SP: sp_GetUserActivitySummary)
    [HttpGet("users/{userId:long}/activity")]
    public async Task<IActionResult> GetUserActivitySummary([FromRoute] long userId, CancellationToken ct)
    {
        var data = await _reports.GetUserActivitySummaryAsync(userId, ct);
        return data is null ? NoContent() : Ok(data); // veri yoksa 204, hata varsa middleware 5xx döner
    }

    // Son 30 günde çoklu kablolar aylık rapor
    [HttpGet("multi/monthly")]
    public async Task<IActionResult> GetMonthlyReportForMultiCables(CancellationToken ct)
    {
        var list = await _reports.GetMonthlyReportForMultiCablesAsync(ct);
        return (list is null || list.Count == 0) ? NoContent() : Ok(list);
    }

    // Son 30 günde tekli kablolar aylık rapor
    [HttpGet("single/monthly")]
    public async Task<IActionResult> GetMonthlyReportForSingleCables(CancellationToken ct)
    {
        var list = await _reports.GetMonthlyReportForSingleCablesAsync(ct);
        return (list is null || list.Count == 0) ? NoContent() : Ok(list);
    }
}