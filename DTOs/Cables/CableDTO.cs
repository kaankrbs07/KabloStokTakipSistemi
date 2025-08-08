namespace KabloStokTakipSistemi.DTOs.Cables;

// Tekli Kablo DTO’ları

public record CreateSingleCableDto(
    string Color,
    bool IsActive,
    int? MultiCableID
);

public record GetSingleCableDto(
    int CableID,
    string Color,
    bool IsActive,
    int? MultiCableID
);


public record UpdateSingleCableDto(
    int CableID,
    string? Color,
    bool? IsActive,
    int? MultiCableID
);


// Çoklu Kablo DTO’ları

public record CreateMultipleCableDto(
    string CableName,
    int Quantity,
    bool IsActive
);

public record GetMultipleCableDto(
    int MultiCableID,
    string CableName,
    int Quantity,
    bool IsActive
);


public record UpdateMultipleCableDto(
    int MultiCableID,
    string? CableName,
    int? Quantity,
    bool? IsActive
);
