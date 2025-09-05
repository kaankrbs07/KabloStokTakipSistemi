namespace KabloStokTakipSistemi.DTOs.Users;

using System;
using Microsoft.EntityFrameworkCore;

[Keyless]
public record UserActivitySummaryDto
{   
    [Precision(10, 0)]
    public decimal UserID { get; init; }        // SP: NUMERIC(10,0)
    public string FullName { get; init; } = null!;
    public int TotalEntries { get; init; }
    public int TotalExits { get; init; }
    public int TotalTransactions { get; init; }
    public DateTime? LastTransactionDate { get; init; }
}

