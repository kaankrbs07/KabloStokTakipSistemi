using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.DTOs.Cables;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Middlewares;

namespace KabloStokTakipSistemi.Services.Implementations
{
    public class CableService : ICableService
    {
        private readonly AppDbContext _db;
        private readonly IAlertService _alerts;

        public CableService(AppDbContext db, ILogger<CableService> _, IAlertService alerts /*: DI imzası aynı kalsın*/)
        {
            _db = db;
            _alerts = alerts;
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

            return s is null
                ? null
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

            return m is null
                ? null
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
        public async Task<bool> InsertStockMovementAsync(CreateStockMovementDto dto)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // Validasyonlar
                if (dto.MovementType != "Giriş" && dto.MovementType != "Çıkış")
                    throw new ArgumentException("Geçersiz hareket türü. Giriş veya Çıkış olmalıdır.");

                if (dto.TableName != "Single" && dto.TableName != "Multi")
                    throw new ArgumentException("Tablo adı yalnızca 'Single' veya 'Multi' olabilir.");

                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.UserID == dto.UserID && u.IsActive);

                if (user == null)
                    throw new ArgumentException("Geçersiz veya pasif kullanıcı.");

                if (dto.TableName == "Single" && string.IsNullOrEmpty(dto.color))
                    throw new ArgumentException("Single kablo işlemleri için renk bilgisi gereklidir.");

                if (dto.TableName == "Single")
                {
                    if (dto.MovementType == "Giriş")
                    {
                        // Her kablo için ayrı satır ekleme
                        for (int i = 0; i < dto.Quantity; i++)
                        {
                            var singleCable = new SingleCable
                            {
                                Color = dto.color,
                                IsActive = true,
                                MultiCableID = null
                            };
                            _db.SingleCables.Add(singleCable);
                        }
                    }
                    else if (dto.MovementType == "Çıkış")
                    {
                        // En düşük CableID'den başlayarak pasif yapma
                        var cablesToDeactivate = await _db.SingleCables
                            .Where(sc => sc.Color == dto.color && sc.IsActive)
                            .OrderBy(sc => sc.CableID)
                            .Take(dto.Quantity)
                            .ToListAsync();

                        if (cablesToDeactivate.Count < dto.Quantity)
                            throw new InvalidOperationException("İstenen miktarda aktif kablo bulunamadı.");

                        foreach (var cable in cablesToDeactivate)
                        {
                            cable.IsActive = false;
                        }
                    }
                }
                else if (dto.TableName == "Multi")
                {
                    var multiCable = await _db.MultipleCables
                        .FirstOrDefaultAsync(mc => mc.MultiCableID == dto.CableID && mc.IsActive);

                    if (multiCable == null)
                        throw new ArgumentException("Geçersiz veya pasif çoklu kablo ID.");

                    if (dto.MovementType == "Giriş")
                    {
                        multiCable.Quantity += dto.Quantity;
                    }
                    else if (dto.MovementType == "Çıkış")
                    {
                        if (multiCable.Quantity < dto.Quantity)
                            throw new InvalidOperationException($"Yetersiz stok. Mevcut miktar: {multiCable.Quantity}");

                        multiCable.Quantity -= dto.Quantity;
                    }
                }

                // Stok hareketlerini kaydet
                var stockMovement = new StockMovement
                {
                    CableID = dto.CableID,
                    TableName = dto.TableName,
                    Quantity = dto.Quantity,
                    MovementType = dto.MovementType,
                    UserID = dto.UserID,
                    color = dto.TableName == "Single" ? dto.color : null,
                    MovementDate = DateTime.Now
                };

                _db.StockMovements.Add(stockMovement);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Otomatik uyarı tetikleme
                await TriggerAutoAlertIfNeededAsync(dto);

                return true; // İşlem başarılı
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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
        }
    }
}