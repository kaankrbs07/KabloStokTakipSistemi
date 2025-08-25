using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;

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

        // Arayüzde tanımlanan metodu doğru şekilde uyguluyoruz
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
        }
    }
}
