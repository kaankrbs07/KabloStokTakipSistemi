using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Middlewares; // AppErrors, AppException

namespace KabloStokTakipSistemi.Services.Implementations
{
    public sealed class StockMovementService : IStockMovementService
    {
        private readonly AppDbContext _db;
        private readonly IAlertService _alerts;

        public StockMovementService(AppDbContext db, IAlertService alerts)
        {
            _db = db;
            _alerts = alerts;
        }

        // Tek bir stok hareketini işler (Single/Multi + giriş/çıkış)
        public async Task<bool> InsertStockMovementAsync(CreateStockMovementDto dto)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // ---- Validasyonlar ----
                if (dto.MovementType is not ("Giriş" or "Çıkış"))
                    throw new AppException(AppErrors.Validation.BadRequest, "Geçersiz hareket türü. 'Giriş' veya 'Çıkış' olmalıdır.");

                if (dto.TableName is not ("Single" or "Multi"))
                    throw new AppException(AppErrors.Validation.BadRequest, "Geçersiz tablo adı. Yalnızca 'Single' veya 'Multi' olabilir.");

                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == dto.UserID && u.IsActive);
                if (user is null)
                    throw new AppException(AppErrors.Common.NotFound, "Geçersiz veya pasif kullanıcı.");

                if (dto.TableName == "Single" && string.IsNullOrWhiteSpace(dto.color))
                    throw new AppException(AppErrors.Validation.BadRequest, "Single kablo işlemleri için 'Color' zorunludur.");

                // ---- İş mantığı ----
                if (dto.TableName == "Single")
                {
                    if (dto.MovementType == "Giriş")
                    {
                        // Tekli giriş: Quantity kadar yeni aktif satır
                        for (int i = 0; i < dto.Quantity; i++)
                        {
                            _db.SingleCables.Add(new SingleCable
                            {
                                Color = dto.color,
                                IsActive = true,
                                MultiCableID = null
                            });
                        }
                    }
                    else // Çıkış
                    {
                        var toDeactivate = await _db.SingleCables
                            .Where(sc => sc.Color == dto.color && sc.IsActive)
                            .OrderBy(sc => sc.CableID)
                            .Take(dto.Quantity)
                            .ToListAsync();

                        if (toDeactivate.Count < dto.Quantity)
                            throw new AppException(AppErrors.Common.Conflict, "İstenen miktarda aktif Single stok bulunamadı.");

                        foreach (var c in toDeactivate) c.IsActive = false;
                    }
                }
                else // Multi
                {
                    var multi = await _db.MultipleCables
                        .FirstOrDefaultAsync(mc => mc.MultiCableID == dto.CableID && mc.IsActive);

                    if (multi is null)
                        throw new AppException(AppErrors.Common.NotFound, "Geçersiz veya pasif MultiCableID.");

                    if (dto.MovementType == "Giriş")
                    {
                        multi.Quantity += dto.Quantity;
                    }
                    else // Çıkış
                    {
                        if (multi.Quantity < dto.Quantity)
                            throw new AppException(AppErrors.Common.Conflict, $"Yetersiz Multi stok. Mevcut: {multi.Quantity}, İstenen: {dto.Quantity}");
                        multi.Quantity -= dto.Quantity;
                    }
                }

                // ---- Hareket kaydı ----
                _db.StockMovements.Add(new StockMovement
                {
                    CableID = dto.CableID,
                    TableName = dto.TableName,
                    Quantity = dto.Quantity,
                    MovementType = dto.MovementType,
                    UserID = dto.UserID,
                    color = dto.TableName == "Single" ? dto.color : null,
                    MovementDate = DateTime.Now
                });

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Commit sonrası uyarı değerlendirmesi
                await TriggerAutoAlertIfNeededAsync(dto);

                return true;
            }
            catch (AppException)
            {
                await transaction.RollbackAsync();
                throw; // standartlaştırılmış hata
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Beklenmeyen durumları da standart kodla dışarı aktar
                throw new AppException(AppErrors.Common.Unexpected, ex.Message);
            }
        }

        // Geçmiş (tüm)
        public async Task<IEnumerable<GetStockMovementDto>> GetHistoryAsync()
        {
            return await _db.Set<GetStockMovementDto>()
                .FromSqlRaw("EXEC dbo.sp_GetStockMovementHistory")
                .AsNoTracking()
                .ToListAsync();
        }

        // Geçmiş (filtreli)
        public async Task<IEnumerable<GetStockMovementDto>> GetHistoryFilteredAsync(StockMovementFilterDto filter)
        {
            var p = new[]
            {
                new SqlParameter("@TableName", (object?)filter.TableName ?? DBNull.Value),
                new SqlParameter("@CableName", (object?)filter.CableName ?? DBNull.Value),
                new SqlParameter("@Color", (object?)filter.Color ?? DBNull.Value),
                new SqlParameter("@UserID", (object?)filter.UserID ?? DBNull.Value),
                new SqlParameter("@DateFrom", (object?)filter.DateFrom ?? DBNull.Value),
                new SqlParameter("@DateTo", (object?)filter.DateTo ?? DBNull.Value),
            };

            return await _db.Set<GetStockMovementDto>()
                .FromSqlRaw(
                    "EXEC dbo.sp_GetStockMovementHistoryFiltered @TableName, @CableName, @Color, @UserID, @DateFrom, @DateTo",
                    p)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<BulkImportResult> ImportFromExcelAsync(
            Stream fileStream,
            bool dryRun,
            decimal userId,
            CancellationToken ct = default)
        {
            var rowsResult = new List<ImportRowResult>();
            int total = 0, processed = 0, skipped = 0;

            using var wb = new XLWorkbook(fileStream);
            var ws = wb.Worksheets.Worksheet(1);

            var expected = new[] { "TableName", "MovementType", "Quantity", "Color", "CableID" };
            for (int i = 0; i < expected.Length; i++)
            {
                var v = ws.Cell(1, i + 1).GetString();
                if (!string.Equals(v, expected[i], StringComparison.OrdinalIgnoreCase))
                    throw new AppException(AppErrors.Validation.BadRequest, $"Beklenen kolon '{expected[i]}', bulunan '{v}'");
            }

            var oldAutoDetect = _db.ChangeTracker.AutoDetectChangesEnabled;
            _db.ChangeTracker.AutoDetectChangesEnabled = !dryRun;

            try
            {
                var uid = Convert.ToInt64(userId); // tip uyuşmazlığı çözümü

                int r = 2;
                while (!ws.Cell(r, 1).IsEmpty())
                {
                    total++;
                    try
                    {
                        var tableName = ws.Cell(r, 1).GetString().Trim();
                        var movementType = ws.Cell(r, 2).GetString().Trim();
                        var qty = ws.Cell(r, 3).GetValue<int>();
                        var color = ws.Cell(r, 4).GetString().Trim();
                        var cableIdCell = ws.Cell(r, 5);
                        int? cableId = cableIdCell.IsEmpty() ? (int?)null : cableIdCell.GetValue<int>();

                        if (tableName is not ("Single" or "Multi"))
                            throw new AppException(AppErrors.Validation.BadRequest, $"Geçersiz TableName: {tableName}");
                        if (movementType is not ("Giriş" or "Çıkış"))
                            throw new AppException(AppErrors.Validation.BadRequest, $"Geçersiz MovementType: {movementType}");
                        if (qty <= 0)
                            throw new AppException(AppErrors.Validation.BadRequest, "Quantity pozitif olmalı.");

                        var userActive = await _db.Users.AnyAsync(u => u.UserID == uid && u.IsActive, ct);
                        if (!userActive)
                            throw new AppException(AppErrors.Common.NotFound, "Geçersiz veya pasif kullanıcı.");

                        if (tableName == "Single")
                        {
                            if (string.IsNullOrWhiteSpace(color))
                                throw new AppException(AppErrors.Validation.BadRequest, "Single için Color zorunludur.");

                            if (movementType == "Çıkış")
                            {
                                var activeCount = await _db.SingleCables
                                    .Where(sc => sc.Color == color && sc.IsActive)
                                    .CountAsync(ct);
                                if (activeCount < qty)
                                    throw new AppException(AppErrors.Common.Conflict,
                                        $"Yetersiz aktif Single stok. Var: {activeCount}, İstenen: {qty}");
                            }
                        }
                        else // Multi
                        {
                            if (cableId is null)
                                throw new AppException(AppErrors.Validation.BadRequest, "Multi için CableID zorunludur.");

                            var multi = await _db.MultipleCables
                                .Where(m => m.MultiCableID == cableId && m.IsActive)
                                .Select(m => new { m.Quantity })
                                .FirstOrDefaultAsync(ct);

                            if (multi is null)
                                throw new AppException(AppErrors.Common.NotFound, "Geçersiz veya pasif MultiCableID.");

                            if (movementType == "Çıkış" && multi.Quantity < qty)
                                throw new AppException(AppErrors.Common.Conflict,
                                    $"Yetersiz Multi stok. Var: {multi.Quantity}, İstenen: {qty}");
                        }

                        if (!dryRun)
                        {
                            int dtoCableId = tableName == "Multi" ? cableId!.Value : 0;
                            var dto = new CreateStockMovementDto(
                                dtoCableId,
                                tableName,
                                qty,
                                movementType,
                                uid,
                                tableName == "Single" ? color : string.Empty
                            );

                            await InsertStockMovementAsync(dto);
                        }

                        processed++;
                        rowsResult.Add(new ImportRowResult(r, true, null));
                    }
                    catch (Exception ex)
                    {
                        skipped++;
                        // Satır hatasında kodu da yaz: "KSTS-xxxx: mesaj"
                        var errMsg = ex is AppException ax ? $"{ax.Error.Code}: {ax.Message}" : ex.Message;
                        rowsResult.Add(new ImportRowResult(r, false, errMsg));
                    }

                    r++;
                }

                return new BulkImportResult(dryRun, total, processed, skipped, rowsResult);
            }
            finally
            {
                _db.ChangeTracker.AutoDetectChangesEnabled = oldAutoDetect;
            }
        }

        // ---- Uyarı tetikleme (commit sonrası) ----
        private async Task TriggerAutoAlertIfNeededAsync(CreateStockMovementDto dto)
        {
            if (dto.TableName.Equals("Single", StringComparison.OrdinalIgnoreCase))
            {
                var color = dto.color;

                var currentQty = await _db.Database
                    .SqlQueryRaw<int>("SELECT dbo.fn_CurrentStock_Single(@Color)",
                        new SqlParameter("@Color", color))
                    .FirstAsync();

                var minThreshold = await _db.Database
                    .SqlQueryRaw<int>("SELECT dbo.fn_MinThreshold_Single(@Color)",
                        new SqlParameter("@Color", color))
                    .FirstAsync();

                await _alerts.EvaluateSingleThresholdAsync(color, currentQty, minThreshold);
            }
            else if (dto.TableName.Equals("Multi", StringComparison.OrdinalIgnoreCase))
            {
                var currentQty = await _db.Database
                    .SqlQueryRaw<int>("SELECT dbo.fn_CurrentStock_Multi(@Id)",
                        new SqlParameter("@Id", dto.CableID))
                    .FirstAsync();

                var minThreshold = await _db.Database
                    .SqlQueryRaw<int>("SELECT dbo.fn_MinThreshold_Multi(@Id)",
                        new SqlParameter("@Id", dto.CableID))
                    .FirstAsync();

                await _alerts.EvaluateMultiThresholdAsync(dto.CableID, currentQty, minThreshold);
            }
        }

        // ---- Import sonuç modelleri ----
        public sealed record ImportRowResult(int Row, bool Success, string? Error);

        public sealed record BulkImportResult(
            bool DryRun,
            int Total,
            int Processed,
            int Skipped,
            List<ImportRowResult> Rows);
    }
}
