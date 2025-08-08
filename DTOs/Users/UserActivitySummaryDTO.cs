namespace KabloStokTakipSistemi.DTOs.Users;

using System;
using Microsoft.EntityFrameworkCore;

[Keyless]
public record UserActivitySummaryDto(
    long UserID,
    string FullName,
    int TotalEntries,
    int TotalExits,
    int TotalTransactions,
    DateTime? LastTransactionDate
);

