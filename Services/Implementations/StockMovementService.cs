using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations
{
    public class StockMovementService : IStockMovementService
    {
        private readonly AppDbContext _db;
        public StockMovementService(AppDbContext db) => _db = db;

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

            await _db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_InsertStockMovement @CableID, @TableName, @Quantity, @MovementType, @UserID", p);

            return true;
        }

        public async Task<IEnumerable<GetStockMovementDto>> GetHistoryAsync()
        {
            return await _db.Set<GetStockMovementDto>()
                .FromSqlRaw("EXEC dbo.sp_GetStockMovementHistory")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<GetStockMovementDto>> GetHistoryFilteredAsync(StockMovementFilterDto f)
        {
            var p = new[]
            {
                new SqlParameter("@TableName", (object?)f.TableName ?? DBNull.Value),
                new SqlParameter("@CableName", (object?)f.CableName ?? DBNull.Value),
                new SqlParameter("@Color",     (object?)f.Color     ?? DBNull.Value),
                new SqlParameter("@UserID",    (object?)f.UserID    ?? DBNull.Value),
                new SqlParameter("@DateFrom",  (object?)f.DateFrom  ?? DBNull.Value),
                new SqlParameter("@DateTo",    (object?)f.DateTo    ?? DBNull.Value),
            };

            return await _db.Set<GetStockMovementDto>()
                .FromSqlRaw("EXEC dbo.sp_GetStockMovementHistoryFiltered @TableName, @CableName, @Color, @UserID, @DateFrom, @DateTo", p)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}


