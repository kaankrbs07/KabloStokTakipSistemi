namespace KabloStokTakipSistemi.DTOs.Cables;

// RENK EŞİKLERİ

public record CreateColorThresholdDto(
    string Color,
    int MinQuantity
);

public record GetColorThresholdDto(
    string Color,
    int MinQuantity
);


public record UpdateColorThresholdDto(
    string Color,
    int? MinQuantity
);


// ÇOKLU KABLO EŞİKLERİ

public record CreateCableThresholdDto(
    int MultiCableID,
    int MinQuantity
);

public record GetCableThresholdDto(
    int MultiCableID,
    string CableName,
    int MinQuantity
);


public record UpdateCableThresholdDto(
    int MultiCableID,
    int? MinQuantity
);

