namespace KabloStokTakipSistemi.DTOs;
using System;

public record UserLogDto(
    int LogID,
    string FullName,
    string TableName,
    string Operation,
    string Description,
    DateTime LogDate
);