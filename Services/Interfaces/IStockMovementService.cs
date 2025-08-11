using KabloStokTakipSistemi.DTOs;

namespace KabloStokTakipSistemi.Services.Interfaces
{
    public interface IStockMovementService
    {
        Task<bool> InsertAsync(CreateStockMovementDto dto);
        Task<IEnumerable<GetStockMovementDto>> GetHistoryAsync();
        Task<IEnumerable<GetStockMovementDto>> GetHistoryFilteredAsync(StockMovementFilterDto filter);
    }
}