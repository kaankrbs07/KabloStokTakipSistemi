namespace KabloStokTakipSistemi.DTOs;

public record StockMovementFilterDto(
    string?   TableName,   // "Single" | "Multi" | null
    string?   CableName,   // Multi için
    string?   Color,       // Single için
    long?     UserID,      // numeric(10,0)
    DateTime? DateFrom,
    DateTime? DateTo
);