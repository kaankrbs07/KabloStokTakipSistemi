// Services/ReportService.cs
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class ReportService : IReportService
{
    private readonly AppDbContext _db;

    // DI imzasını bozma
    public ReportService(AppDbContext db, ILogger<ReportService> _ /*unused*/)
    {
        _db = db;
    }

    public async Task<UserActivitySummaryDto?> GetUserActivitySummaryAsync(
        decimal userId, CancellationToken ct = default)
    {
        // EXEC dbo.sp_GetUserActivitySummary @UserID = {userId}
        var rows = await _db.Database
            .SqlQueryRaw<UserActivitySummaryDto>("EXEC dbo.sp_GetUserActivitySummary @UserID = {0}", userId)
            .ToListAsync(ct);

        return rows.FirstOrDefault(); // veri yoksa null; Controller 204/404 karar verir
    }

    public async Task<IReadOnlyList<MonthlyMultiCableReportDto>> GetMonthlyReportForMultiCablesAsync(
        CancellationToken ct = default)
    {
        // sp_GetMonthlyReport_MultiCables (Color, TotalExitFromMultiCables ... son 30 gün)
        var rows = await _db.Database
            .SqlQueryRaw<MonthlyMultiCableReportDto>("EXEC dbo.sp_GetMonthlyReport_MultiCables")
            .ToListAsync(ct);

        return rows; // boş liste ise Controller 204 verebilir
    }

    public async Task<IReadOnlyList<MonthlySingleCableReportDto>> GetMonthlyReportForSingleCablesAsync(
        CancellationToken ct = default)
    {
        // sp_GetMonthlyReport_SingleCables (Top 5 renk; Current/PreviousMonthUsage, UsageDifference)
        var rows = await _db.Database
            .SqlQueryRaw<MonthlySingleCableReportDto>("EXEC dbo.sp_GetMonthlyReport_SingleCables")
            .ToListAsync(ct);

        return rows;
    }
}