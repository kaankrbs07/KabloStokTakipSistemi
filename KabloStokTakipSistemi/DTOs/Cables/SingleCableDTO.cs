
namespace KabloStokTakipSistemi.DTOs.Cables;

public record CreateSingleCableDto(
    string Color,
    bool IsActive = true,
    int? MultiCableID = null
);

public record UpdateSingleCableDto(
    int CableID,
    string? Color,
    bool? IsActive,
    int? MultiCableID
);

public record GetSingleCableDto(
    int CableID,
    string Color,
    bool IsActive,
    int? MultiCableID
);