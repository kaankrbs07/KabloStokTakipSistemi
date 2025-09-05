namespace KabloStokTakipSistemi.DTOs;

public record MonthlyMultiCableReportDto
{
    public string Color { get; init; } = null!;
    public int TotalExitFromMultiCables { get; init; }   // sp_GetMonthlyReport_MultiCables SELECT kolonu
}

public record MonthlySingleCableReportDto
{
    public string Color { get; init; } = null!;
    public int CurrentMonthUsage { get; init; }
    public int PreviousMonthUsage { get; init; }
    public int UsageDifference { get; init; }
}
