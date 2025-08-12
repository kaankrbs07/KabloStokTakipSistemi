using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs.Cables;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations
{
    public class CableService : ICableService
    {
        private readonly AppDbContext _db;
        public CableService(AppDbContext db) => _db = db;

        // ===================== SINGLE =====================
        public async Task<IEnumerable<GetSingleCableDto>> GetAllSingleCablesAsync()
        {
            return await _db.SingleCables
                .AsNoTracking()
                .Select(s => new GetSingleCableDto(
                    s.CableID,
                    s.Color,
                    s.IsActive,
                    s.MultiCableID 
                ))
                .ToListAsync();
        }

        public async Task<GetSingleCableDto?> GetSingleCableByIdAsync(int cableId)
        {
            var s = await _db.SingleCables.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CableID == cableId);

            return s is null ? null : new GetSingleCableDto(
                s.CableID,
                s.Color,
                s.IsActive,
                s.MultiCableID 
            );
        }

        public async Task<bool> CreateSingleCableAsync(CreateSingleCableDto dto)
        {
            var entity = new SingleCable
            {
                Color = dto.Color,
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
            // SP: dbo.sp_GetInactiveSingleCables
            return await _db.Set<GetSingleCableDto>()
                .FromSqlRaw("EXEC dbo.sp_GetInactiveSingleCables")
                .AsNoTracking()
                .ToListAsync();
        }

        // ====================== MULTI ======================
        public async Task<IEnumerable<GetMultiCableDto>> GetAllMultiCablesAsync()
        {
            return await _db.MultipleCables
                .AsNoTracking()
                .Select(m => new GetMultiCableDto(
                    m.MultiCableID,
                    m.CableName,   
                    m.Quantity,
                    m.IsActive
                ))
                .ToListAsync();
        }

        public async Task<GetMultiCableDto?> GetMultiCableByIdAsync(int multiCableId)
        {
            var m = await _db.MultipleCables.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MultiCableID == multiCableId);

            return m is null ? null : new GetMultiCableDto(
                m.MultiCableID,
                m.CableName,
                m.Quantity,
                m.IsActive
            );
        }

        public async Task<bool> CreateMultiCableAsync(CreateMultiCableDto dto)
        {
            var entity = new MultiCable
            {
                CableName = dto.CableName,
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
            // SP: dbo.sp_GetInactiveMultipleCables  (liste bu isimde; mevcut olanı kullanıyoruz)
            return await _db.Set<GetMultiCableDto>()
                .FromSqlRaw("EXEC dbo.sp_GetInactiveMultipleCables")
                .AsNoTracking()
                .ToListAsync();
        }

        // ============== MULTI CONTENT ==============
        public async Task<IEnumerable<GetMultiCableContentDto>> GetMultiCableContentsAsync(int multiCableId)
        {
            // SP: dbo.sp_GetMultiCableContentDetails @MultiCableID
            var p = new[] { new SqlParameter("@MultiCableID", multiCableId) };

            return await _db.Set<GetMultiCableContentDto>()
                .FromSqlRaw("EXEC dbo.sp_GetMultiCableContentDetails @MultiCableID", p)
                .AsNoTracking()
                .ToListAsync();
        }

        // =================== STOCK MOVEMENTS ====================
        public async Task<bool> InsertStockMovementAsync(int cableId, string tableName, int quantity, string movementType, int userId)
        {
            // SP: dbo.sp_InsertStockMovement @CableID, @TableName, @Quantity, @MovementType, @UserID
            var p = new[]
            {
                new SqlParameter("@CableID", cableId),
                new SqlParameter("@TableName", tableName),       // 'Single' | 'Multi'
                new SqlParameter("@Quantity", quantity),
                new SqlParameter("@MovementType", movementType), // 'Giriş' | 'Çıkış'
                new SqlParameter("@UserID", userId)
            };

            await _db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_InsertStockMovement @CableID, @TableName, @Quantity, @MovementType, @UserID", p);

            return true;
        }

        // =================== EXTRA: COLOR STATUS =================
        public async Task<int> GetStockStatusByColorAsync(string color)
        {
            // SP: dbo.sp_GetStockStatusByColor @Color  -> Toplam/uygun sütunu döndürüyor varsayımıyla ilk kolonu int okuyoruz.
            var p = new[] { new SqlParameter("@Color", color) };
            var rows = await _db.Set<TempColorStatusRow>()
                .FromSqlRaw("EXEC dbo.sp_GetStockStatusByColor @Color", p)
                .AsNoTracking()
                .ToListAsync();

            return rows.FirstOrDefault()?.Total ?? 0;
        }

        private sealed class TempColorStatusRow
        {
            public int Total { get; set; }
        }
    }
}

