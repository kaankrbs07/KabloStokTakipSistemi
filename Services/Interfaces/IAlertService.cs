using KabloStokTakipSistemi.DTOs;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IAlertService
{
    // --- Listeleme ---
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

    // --- Durum değişiklikleri ---
    Task<bool> ResolveAsync(int alertId, string? resolveNote = null, CancellationToken ct = default);
    Task<bool> ReactivateAsync(int alertId, string? reason = null, CancellationToken ct = default);

    // --- Aktiflik kontrolü ---
    Task<bool> HasActiveAlertForColorAsync(string color, CancellationToken ct = default);
    Task<bool> HasActiveAlertForMultiAsync(int multiCableId, CancellationToken ct = default);

    // --- E-posta bildirimleri (mevcut) ---
    Task<bool> NotifyAdminsForAlertAsync(int alertId, CancellationToken ct = default);
    Task<bool> NotifyAdminsForLowStockAsync(string color, int qty, CancellationToken ct = default);

    // --- OTOMATİK TETİK GİRİŞ NOKTALARI ---
    Task<ThresholdEvaluationResult> EvaluateSingleThresholdAsync(
        string color, int currentQty, int minThreshold, CancellationToken ct = default);

    Task<ThresholdEvaluationResult> EvaluateMultiThresholdAsync(
        int multiCableId, int currentQty, int minThreshold, CancellationToken ct = default);
}

// DTO: servis döndürsün 
public sealed record ThresholdEvaluationResult(
    bool AlertCreatedAndNotified, // yeni alert açıldı + mail gitti
    bool AlertResolved, // aktif alert kapatıldı
    bool WasAlreadyActive, // zaten aktif alert vardı
    int RecipientCount, // e-posta alan admin sayısı
    int CurrentQty,
    int MinThreshold,
    string Kind, // "Single" | "Multi"
    string Key // Single: Color, Multi: MultiCableId string'i
);