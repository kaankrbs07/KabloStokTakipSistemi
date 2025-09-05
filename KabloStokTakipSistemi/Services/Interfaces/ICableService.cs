using KabloStokTakipSistemi.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using KabloStokTakipSistemi.DTOs.Cables;

namespace KabloStokTakipSistemi.Services.Interfaces
{
    public interface ICableService
    {
        // SINGLE
        Task<IEnumerable<GetSingleCableDto>> GetAllSingleCablesAsync();
        Task<GetSingleCableDto?> GetSingleCableByIdAsync(int cableId);
        Task<bool> CreateSingleCableAsync(CreateSingleCableDto dto);
        Task<bool> DeactivateSingleCableAsync(int cableId);
        Task<IEnumerable<GetSingleCableDto>> GetInactiveSingleCablesAsync();

// MULTI
        Task<IEnumerable<GetMultiCableDto>> GetAllMultiCablesAsync();
        Task<GetMultiCableDto?> GetMultiCableByIdAsync(int multiCableId);
        Task<bool> CreateMultiCableAsync(CreateMultiCableDto dto);
        Task<bool> DeactivateMultiCableAsync(int multiCableId);
        Task<IEnumerable<GetMultiCableDto>> GetInactiveMultiCablesAsync();

// MULTI CONTENT
        Task<IEnumerable<GetMultiCableContentDto>> GetMultiCableContentsAsync(int multiCableId);

// STOCK MOVEMENTS
        Task<bool> InsertStockMovementAsync(CreateStockMovementDto dto);

// COLOR STATUS
        Task<int> GetStockStatusByColorAsync(string color);

// THRESHOLDS
        Task<bool> SetColorThresholdAsync(CreateColorThresholdDto dto);
        Task<bool> SetCableThresholdAsync(CreateCableThresholdDto dto);
        Task<IEnumerable<GetColorThresholdDto>> GetColorThresholdsAsync();
        Task<IEnumerable<GetCableThresholdDto>> GetCableThresholdsAsync();
    }
}