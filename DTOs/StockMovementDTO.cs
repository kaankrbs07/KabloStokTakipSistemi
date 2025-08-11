namespace KabloStokTakipSistemi.DTOs;

public record CreateStockMovementDto(
    int    CableID,
    string TableName,     // "Single" | "Multi"
    int    Quantity,
    string MovementType,  // "Giriş" | "Çıkış"
    long   UserID         // numeric(10,0)
);

public record GetStockMovementDto(
    int      MovementID,
    int      CableID,
    string   TableName,
    int      Quantity,
    string   MovementType,
    DateTime MovementDate,
    long     UserID        // tabloya birebir
);

public record UpdateStockMovementDto(
    int     MovementID,
    int?    CableID,
    string? TableName,
    int?    Quantity,
    string? MovementType,
    long?   UserID
);

