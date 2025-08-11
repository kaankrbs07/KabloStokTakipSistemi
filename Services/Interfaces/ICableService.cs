using KabloStokTakipSistemi.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using KabloStokTakipSistemi.DTOs.Cables;

namespace KabloStokTakipSistemi.Services.Interfaces
{
    public interface ICableService
    {
        // Single
        Task<IEnumerable<GetSingleCableDto>> GetAllSingleCablesAsync();
        Task<GetSingleCableDto?> GetSingleCableByIdAsync(int cableId);
        Task<bool> CreateSingleCableAsync(CreateSingleCableDto dto);
        Task<bool> DeactivateSingleCableAsync(int cableId);

        // Multi
        Task<IEnumerable<GetMultiCableDto>> GetAllMultiCablesAsync();
        Task<GetMultiCableDto?> GetMultiCableByIdAsync(int multiCableId);
        Task<bool> CreateMultiCableAsync(CreateMultiCableDto dto);
        Task<bool> DeactivateMultiCableAsync(int multiCableId);

        // MultiCable contents
        Task<IEnumerable<GetMultiCableContentDto>> GetMultiCableContentsAsync(int multiCableId);
    }
}