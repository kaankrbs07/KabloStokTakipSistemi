namespace KabloStokTakipSistemi.DTOs;

public record GetAlertDto(
    int AlertID,
    string? AlertType,
    DateTime AlertDate,
    string? Color,
    int? MultiCableID,
    int MinQuantity,
    string Description,
    bool IsActive
);
