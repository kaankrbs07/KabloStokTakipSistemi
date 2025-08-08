namespace KabloStokTakipSistemi.DTOs;

public record CableReportDto(
    string Color,
    int CurrentMonthUsage,
    int PreviousMonthUsage,
    int UsageDifference
);

public record MultiCableReportDto(
    string Color,
    int TotalExitFromMultiCables
);