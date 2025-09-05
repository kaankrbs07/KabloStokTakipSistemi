using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Services.Implementations;

namespace KabloStokTakipSistemi.Services.Interfaces
{
    public interface IStockMovementService
    {
        Task<bool> InsertStockMovementAsync(CreateStockMovementDto dto);
        Task<IEnumerable<GetStockMovementDto>> GetHistoryAsync();
        Task<IEnumerable<GetStockMovementDto>> GetHistoryFilteredAsync(StockMovementFilterDto filter);   
        Task<StockMovementService.BulkImportResult> ImportFromExcelAsync(Stream fileStream, bool dryRun, decimal userId, CancellationToken ct = default);
    }
}