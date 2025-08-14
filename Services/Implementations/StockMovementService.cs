using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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

        public async Task<bool> InsertAsync(CreateStockMovementDto dto)
        {
            var p = new[]
            {
                new SqlParameter("@CableID", dto.CableID),
                new SqlParameter("@TableName", dto.TableName),
                new SqlParameter("@Quantity", dto.Quantity),
                new SqlParameter("@MovementType", dto.MovementType),
                new SqlParameter("@UserID", dto.UserID)
            };

            // 1) Hareketi kaydet
            await _db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_InsertStockMovement @CableID, @TableName, @Quantity, @MovementType, @UserID", p);

            // 2) Başarılıysa otomatik uyarı değerlendirmesi
            await TriggerAutoAlertIfNeededAsync(dto);

            return true;
        }

        public async Task<IEnumerable<GetStockMovementDto>> GetHistoryAsync()
        {
            return await _db.Set<GetStockMovementDto>()
                .FromSqlRaw("EXEC dbo.sp_GetStockMovementHistory")
                .AsNoTracking()
                .ToListAsync();
        }

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

        // ---------- PRIVATE ----------
        private async Task TriggerAutoAlertIfNeededAsync(CreateStockMovementDto dto)
        {
            if (dto.TableName.Equals("Single", StringComparison.OrdinalIgnoreCase))
            {
                // CableID -> Color
                var color = await _db.SingleCables
                    .Where(c => c.CableID == dto.CableID)
                    .Select(c => c.Color)
                    .FirstAsync();

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
            // Diğer TableName değerleri varsa burada ele alabilirsiniz.
        }
    }
}

