namespace KabloStokTakipSistemi.DTOs;

public record GetLogDto(
    int LogID,
    string TableName,
    string? Operation,
    string? Description,
    DateTime LogDate,
    string? Username
);
