using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.DTOs.Users;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IReportService
{
    Task<UserActivitySummaryDto?> GetUserActivitySummaryAsync(decimal userId, CancellationToken ct = default);

    Task<IReadOnlyList<MonthlyMultiCableReportDto>> GetMonthlyReportForMultiCablesAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<MonthlySingleCableReportDto>> GetMonthlyReportForSingleCablesAsync(
        CancellationToken ct = default);
}