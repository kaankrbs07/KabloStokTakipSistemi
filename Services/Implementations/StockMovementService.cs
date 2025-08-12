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
        private readonly ILogger<StockMovementService> _logger;
        
        public StockMovementService(AppDbContext db, ILogger<StockMovementService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> InsertAsync(CreateStockMovementDto dto)
        {
            try
            {
                _logger.LogInformation("Inserting stock movement - CableID: {CableId}, TableName: {TableName}, Quantity: {Quantity}, MovementType: {MovementType}, UserID: {UserId}", 
                    dto.CableID, dto.TableName, dto.Quantity, dto.MovementType, dto.UserID);
                
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

                _logger.LogInformation("Successfully inserted stock movement for CableID: {CableId}", dto.CableID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting stock movement for CableID: {CableId}", dto.CableID);
                throw;
            }
        }

        public async Task<IEnumerable<GetStockMovementDto>> GetHistoryAsync()
        {
            try
            {
                _logger.LogInformation("Getting stock movement history from database");
                var result = await _db.Set<GetStockMovementDto>()
                    .FromSqlRaw("EXEC dbo.sp_GetStockMovementHistory")
                    .AsNoTracking()
                    .ToListAsync();
                
                _logger.LogInformation("Retrieved {Count} stock movements from history", result.Count());
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock movement history from database");
                throw;
            }
        }

        public async Task<IEnumerable<GetStockMovementDto>> GetHistoryFilteredAsync(StockMovementFilterDto f)
        {
            try
            {
                _logger.LogInformation("Getting filtered stock movement history - TableName: {TableName}, CableName: {CableName}, Color: {Color}, UserID: {UserId}", 
                    f.TableName, f.CableName, f.Color, f.UserID);
                
                var p = new[]
                {
                    new SqlParameter("@TableName", (object?)f.TableName ?? DBNull.Value),
                    new SqlParameter("@CableName", (object?)f.CableName ?? DBNull.Value),
                    new SqlParameter("@Color", (object?)f.Color ?? DBNull.Value),
                    new SqlParameter("@UserID", (object?)f.UserID ?? DBNull.Value),
                    new SqlParameter("@DateFrom", (object?)f.DateFrom ?? DBNull.Value),
                    new SqlParameter("@DateTo", (object?)f.DateTo ?? DBNull.Value),
                };

                var result = await _db.Set<GetStockMovementDto>()
                    .FromSqlRaw(
                        "EXEC dbo.sp_GetStockMovementHistoryFiltered @TableName, @CableName, @Color, @UserID, @DateFrom, @DateTo",
                        p)
                    .AsNoTracking()
                    .ToListAsync();
                
                _logger.LogInformation("Retrieved {Count} filtered stock movements", result.Count());
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered stock movement history");
                throw;
            }
        }
    }
}