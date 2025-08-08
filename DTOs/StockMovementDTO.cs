namespace KabloStokTakipSistemi.DTOs;

public record CreateStockMovementDto(
    int CableID,
    string TableName,
    int Quantity,
    string MovementType,
    long UserID
);

public record GetStockMovementDto(
    int MovementID,
    int CableID,
    string TableName,
    int Quantity,
    string MovementType,
    DateTime MovementDate,
    string? UserName
);
 
public record UpdateStockMovementDto(
    int MovementID,
    int? CableID,
    string? TableName,
    int? Quantity,
    string? MovementType,
    long? UserID
);
