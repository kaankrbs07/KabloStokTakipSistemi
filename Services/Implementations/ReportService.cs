using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReportService> _logger;

    public ReportService(AppDbContext db, ILogger<ReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UserActivitySummaryDto?> GetUserActivitySummaryAsync(
        decimal userId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting user activity summary for user ID: {UserId}", userId);
            
            // EXEC dbo.sp_GetUserActivitySummary @UserID = {userId}
            var rows = await _db.Database
                .SqlQueryRaw<UserActivitySummaryDto>(
                    "EXEC dbo.sp_GetUserActivitySummary @UserID = {0}", userId)
                .ToListAsync(ct);

            var result = rows.FirstOrDefault();
            if (result == null)
            {
                _logger.LogWarning("User activity summary not found for user ID: {UserId}", userId);
            }
            else
            {
                _logger.LogInformation("Retrieved user activity summary for user ID: {UserId}", userId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity summary for user ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<IReadOnlyList<MonthlyMultiCableReportDto>> GetMonthlyReportForMultiCablesAsync(
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting monthly report for multi cables");
            
            // Kolonlar: Color, TotalExitFromMultiCables (son 30 gün, Multi & Çıkış) 
            // sp_GetMonthlyReport_MultiCables
            var rows = await _db.Database
                .SqlQueryRaw<MonthlyMultiCableReportDto>(
                    "EXEC dbo.sp_GetMonthlyReport_MultiCables")
                .ToListAsync(ct);

            _logger.LogInformation("Retrieved {Count} multi cable monthly report records", rows.Count);
            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly report for multi cables");
            throw;
        }
    }

    public async Task<IReadOnlyList<MonthlySingleCableReportDto>> GetMonthlyReportForSingleCablesAsync(
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting monthly report for single cables");
            
            // Top 5 renk; CurrentMonthUsage, PreviousMonthUsage, UsageDifference kolonları
            // sp_GetMonthlyReport_SingleCables
            var rows = await _db.Database
                .SqlQueryRaw<MonthlySingleCableReportDto>(
                    "EXEC dbo.sp_GetMonthlyReport_SingleCables")
                .ToListAsync(ct);

            _logger.LogInformation("Retrieved {Count} single cable monthly report records", rows.Count);
            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly report for single cables");
            throw;
        }
    }
}

