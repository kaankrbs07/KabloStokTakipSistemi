// Services/CableService.cs
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Cables;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Middlewares; // İstersen AppException kullan

namespace KabloStokTakipSistemi.Services.Implementations
{
    public class CableService : ICableService
    {
        private readonly AppDbContext _db;
        public CableService(AppDbContext db, ILogger<CableService> _ /*silme: DI imzası aynı kalsın*/)
        {
            _db = db;
        }

        // -------- SINGLE --------
        public async Task<IEnumerable<GetSingleCableDto>> GetAllSingleCablesAsync()
        {
            return await _db.SingleCables
                .AsNoTracking()
                .Select(s => new GetSingleCableDto(
                    s.CableID, s.Color, s.IsActive, s.MultiCableID))
                .ToListAsync();
        }

        public async Task<GetSingleCableDto?> GetSingleCableByIdAsync(int cableId)
        {
            var s = await _db.SingleCables.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CableID == cableId);

            return s is null ? null
                : new GetSingleCableDto(s.CableID, s.Color, s.IsActive, s.MultiCableID);
        }

        public async Task<bool> CreateSingleCableAsync(CreateSingleCableDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Color))
                throw new AppException(AppErrors.Validation.BadRequest, "Color boş olamaz.");

            var entity = new SingleCable
            {
                Color = dto.Color.Trim(),
                IsActive = dto.IsActive,
                MultiCableID = dto.MultiCableID
            };

            _db.SingleCables.Add(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateSingleCableAsync(int cableId)
        {
            var s = await _db.SingleCables.FindAsync(cableId);
            if (s is null) return false;
            s.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<GetSingleCableDto>> GetInactiveSingleCablesAsync()
        {
            return await _db.Set<GetSingleCableDto>()
                .FromSqlRaw("EXEC dbo.sp_GetInactiveSingleCables")
                .AsNoTracking()
                .ToListAsync();
        }

        // -------- MULTI --------
        public async Task<IEnumerable<GetMultiCableDto>> GetAllMultiCablesAsync()
        {
            return await _db.MultipleCables
                .AsNoTracking()
                .Select(m => new GetMultiCableDto(m.MultiCableID, m.CableName, m.Quantity, m.IsActive))
                .ToListAsync();
        }

        public async Task<GetMultiCableDto?> GetMultiCableByIdAsync(int multiCableId)
        {
            var m = await _db.MultipleCables.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MultiCableID == multiCableId);

            return m is null ? null
                : new GetMultiCableDto(m.MultiCableID, m.CableName, m.Quantity, m.IsActive);
        }

        public async Task<bool> CreateMultiCableAsync(CreateMultiCableDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CableName))
                throw new AppException(AppErrors.Validation.BadRequest, "CableName boş olamaz.");

            var entity = new MultiCable
            {
                CableName = dto.CableName.Trim(),
                Quantity = dto.Quantity,
                IsActive = dto.IsActive
            };

            _db.MultipleCables.Add(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateMultiCableAsync(int multiCableId)
        {
            var m = await _db.MultipleCables.FindAsync(multiCableId);
            if (m is null) return false;
            m.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<GetMultiCableDto>> GetInactiveMultiCablesAsync()
        {
            return await _db.Set<GetMultiCableDto>()
                .FromSqlRaw("EXEC dbo.sp_GetInactiveMultipleCables")
                .AsNoTracking()
                .ToListAsync();
        }

        // -------- MULTI CONTENT --------
        public async Task<IEnumerable<GetMultiCableContentDto>> GetMultiCableContentsAsync(int multiCableId)
        {
            var p = new[] { new SqlParameter("@MultiCableID", multiCableId) };
            return await _db.Set<GetMultiCableContentDto>()
                .FromSqlRaw("EXEC dbo.sp_GetMultiCableContentDetails @MultiCableID", p)
                .AsNoTracking()
                .ToListAsync();
        }

        // -------- STOCK MOVEMENTS --------
        public async Task<bool> InsertStockMovementAsync(int cableId, string tableName, int quantity, string movementType, long userId)
        {
            var p = new[]
            {
                new SqlParameter("@CableID", cableId),
                new SqlParameter("@TableName", tableName),
                new SqlParameter("@Quantity", quantity),
                new SqlParameter("@MovementType", movementType),
                new SqlParameter("@UserID", userId)
            };

            await _db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_InsertStockMovement @CableID, @TableName, @Quantity, @MovementType, @UserID", p);

            return true;
        }

        // -------- THRESHOLDS --------
        public async Task<bool> SetColorThresholdAsync(CreateColorThresholdDto dto)
        {
            var p = new[]
            {
                new SqlParameter("@Color", dto.Color),
                new SqlParameter("@MinQuantity", dto.MinQuantity)
            };
            await _db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_SetColorThreshold @Color, @MinQuantity", p);
            return true;
        }

        public async Task<bool> SetCableThresholdAsync(CreateCableThresholdDto dto)
        {
            var p = new[]
            {
                new SqlParameter("@MultiCableID", dto.MultiCableID),
                new SqlParameter("@MinQuantity", dto.MinQuantity)
            };
            await _db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_SetCableThreshold @MultiCableID, @MinQuantity", p);
            return true;
        }

        public async Task<IEnumerable<GetColorThresholdDto>> GetColorThresholdsAsync()
        {
            return await _db.Set<GetColorThresholdDto>()
                .FromSqlRaw("SELECT Color, MinQuantity FROM ColorThresholds")
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<GetCableThresholdDto>> GetCableThresholdsAsync()
        {
            return await _db.Set<GetCableThresholdDto>()
                .FromSqlRaw(@"SELECT c.MultiCableID, mc.CableName, c.MinQuantity
                              FROM CableThresholds c
                              JOIN MultipleCables mc ON mc.MultiCableID = c.MultiCableID")
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<int> GetStockStatusByColorAsync(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                throw new AppException(AppErrors.Validation.BadRequest, "Color boş olamaz.");

            var p = new[] { new SqlParameter("@Color", color.Trim()) };
            var result = await _db.Database
                .SqlQueryRaw<int>("EXEC dbo.sp_GetStockStatusByColor @Color", p)
                .FirstAsync();
            return result;
        }

    }
}
