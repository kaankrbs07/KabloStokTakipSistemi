using KabloStokTakipSistemi.DTOs;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IAlertService
{
    // Listeleme + filtreler (tamamı opsiyonel)
    Task<IReadOnlyList<GetAlertDto>> GetAlertsAsync(
        bool? isActive = null,
        string? alertType = null,
        string? color = null,
        int? multiCableId = null,
        DateTime? from = null,
        DateTime? to = null,
        int? skip = null,
        int? take = null,
        CancellationToken ct = default);

    Task<GetAlertDto?> GetByIdAsync(int alertId, CancellationToken ct = default);

    // Çözülmüş kabul et (IsActive = 0) – açıklamaya not düşer
    Task<bool> ResolveAsync(int alertId, string? resolveNote = null, CancellationToken ct = default);

    // Tekrar aktif et
    Task<bool> ReactivateAsync(int alertId, string? reason = null, CancellationToken ct = default);

    // DB fonksiyonu: belirli renkte aktif uyarı var mı?
    Task<bool> HasActiveAlertForColorAsync(string color, CancellationToken ct = default);
}