using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<UserActivitySummaryDto?> GetUserActivitySummaryAsync(
        decimal userId, CancellationToken ct = default)
    {
        // EXEC dbo.sp_GetUserActivitySummary @UserID = {userId}
        var rows = await _db.Database
            .SqlQueryRaw<UserActivitySummaryDto>(
                "EXEC dbo.sp_GetUserActivitySummary @UserID = {0}", userId)
            .ToListAsync(ct);

        return rows.FirstOrDefault();
    }

    public async Task<IReadOnlyList<MonthlyMultiCableReportDto>> GetMonthlyReportForMultiCablesAsync(
        CancellationToken ct = default)
    {
        // Kolonlar: Color, TotalExitFromMultiCables (son 30 gün, Multi & Çıkış) 
        // sp_GetMonthlyReport_MultiCables
        var rows = await _db.Database
            .SqlQueryRaw<MonthlyMultiCableReportDto>(
                "EXEC dbo.sp_GetMonthlyReport_MultiCables")
            .ToListAsync(ct);

        return rows;
    }

    public async Task<IReadOnlyList<MonthlySingleCableReportDto>> GetMonthlyReportForSingleCablesAsync(
        CancellationToken ct = default)
    {
        // Top 5 renk; CurrentMonthUsage, PreviousMonthUsage, UsageDifference kolonları
        // sp_GetMonthlyReport_SingleCables
        var rows = await _db.Database
            .SqlQueryRaw<MonthlySingleCableReportDto>(
                "EXEC dbo.sp_GetMonthlyReport_SingleCables")
            .ToListAsync(ct);

        return rows;
    }
}

