namespace KabloStokTakipSistemi.DTOs
{
    // Logs tablosu kolonları: LogID, TableName, Operation, Description, UserID, LogDate
    public sealed record LogDto(
        int LogID,
        string TableName,
        string Operation,
        string Description,
        int UserID,
        DateTime LogDate
    );

    public sealed record LogFilterDto(
        DateTime? FromDate,
        DateTime? ToDate,
        int? UserID,
        string? TableName,
        string? Operation,
        string? Search,        // Description içinde arama
        int Page = 1,
        int PageSize = 25,
        bool Desc = true       // tarihe göre sıralama
    );

    public sealed record LogStatDto(
        string Key,            // Operation ya da TableName
        int Count
    );

    public sealed record ManualStockEditLogDto(
        int CableID,
        string TableName,      // "Multi" bekleniyor (SP kısıtı)
        int OldQuantity,
        int NewQuantity,
        int EditedByUserID,
        string Reason
    );

    public sealed record PagedResult<T>(
        IReadOnlyList<T> Items,
        int TotalCount,
        int Page,
        int PageSize
    );
}