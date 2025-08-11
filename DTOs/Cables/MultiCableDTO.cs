
namespace KabloStokTakipSistemi.DTOs.Cables;

public record CreateMultiCableDto(
    string CableName,
    int Quantity,
    bool IsActive = true
);

public record UpdateMultiCableDto(
    int MultiCableID,
    string? CableName,
    int? Quantity,
    bool? IsActive
);

public record GetMultiCableDto(
    int MultiCableID,
    string CableName,
    int Quantity,
    bool IsActive
);