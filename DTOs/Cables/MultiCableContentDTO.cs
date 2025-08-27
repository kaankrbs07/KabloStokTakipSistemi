
namespace KabloStokTakipSistemi.DTOs.Cables;

public record CreateMultiCableContentDto(
    int MultiCableID,
    int SingleCableID,
    int Quantity
);

public record GetMultiCableContentDto(
    int MultiCableID,
    int SingleCableID,
    string? SingleCableColor,   
    int Quantity
);
