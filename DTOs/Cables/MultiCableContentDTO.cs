// DTOs/Cables/MultiCableContentDtos.cs
namespace KabloStokTakipSistemi.DTOs.Cables;

public record CreateMultiCableContentDto(
    int MultiCableID,
    int SingleCableID,
    int Quantity
);

public record GetMultiCableContentDto(
    int MultiCableID,
    int SingleCableID,
    string? SingleCableColor,   // rapor/kullanıcı arayüzü için faydalı
    int Quantity
);