using AutoMapper;
using AutoMapper.QueryableExtensions;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class AlertService : IAlertService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly IEmailService _email;
    private const string AdminRole = "Admin";

    public AlertService(AppDbContext db, IMapper mapper, IEmailService email)
    {
        _db = db;
        _mapper = mapper;
        _email = email;
    }

    // -------- QUERIES --------
    public async Task<IReadOnlyList<GetAlertDto>> GetAlertsAsync(
        bool? isActive = null, string? alertType = null, string? color = null, int? multiCableId = null,
        DateTime? from = null, DateTime? to = null, int? skip = null, int? take = null, CancellationToken ct = default)
    {
        IQueryable<Alert> q = _db.Alerts.AsNoTracking();
        if (isActive is not null) q = q.Where(a => a.IsActive == isActive);
        if (!string.IsNullOrWhiteSpace(alertType)) q = q.Where(a => a.AlertType == alertType);
        if (!string.IsNullOrWhiteSpace(color)) q = q.Where(a => a.Color == color);
        if (multiCableId is not null) q = q.Where(a => a.MultiCableID == multiCableId);
        if (from is not null) q = q.Where(a => a.AlertDate >= from);
        if (to is not null) q = q.Where(a => a.AlertDate <= to);
        q = q.OrderByDescending(a => a.AlertDate).ThenBy(a => a.AlertID);
        if (skip is not null) q = q.Skip(skip.Value);
        if (take is not null) q = q.Take(take.Value);

        return await q.ProjectTo<GetAlertDto>(_mapper.ConfigurationProvider).ToListAsync(ct);
    }

    public async Task<GetAlertDto?> GetByIdAsync(int alertId, CancellationToken ct = default)
    {
        return await _db.Alerts.AsNoTracking()
            .Where(a => a.AlertID == alertId)
            .ProjectTo<GetAlertDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);
    }

    // -------- STATE CHANGES --------
    public async Task<bool> ResolveAsync(int alertId, string? resolveNote = null, CancellationToken ct = default)
    {
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.AlertID == alertId, ct);
        if (alert is null) return false;
        if (!alert.IsActive) return true;

        alert.IsActive = false;
        if (!string.IsNullOrWhiteSpace(resolveNote))
        {
            var suffix = $" [KAPATILDI: {DateTime.Now:yyyy-MM-dd HH:mm}; Not: {resolveNote}]";
            var target = (alert.Description ?? string.Empty) + suffix;
            alert.Description = target.Length > 255 ? target[..255] : target;
        }
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReactivateAsync(int alertId, string? reason = null, CancellationToken ct = default)
    {
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.AlertID == alertId, ct);
        if (alert is null) return false;
        if (alert.IsActive) return true;

        alert.IsActive = true;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            var suffix = $" [TEKRAR AKTİF: {DateTime.Now:yyyy-MM-dd HH:mm}; Sebep: {reason}]";
            var target = (alert.Description ?? string.Empty) + suffix;
            alert.Description = target.Length > 255 ? target[..255] : target;
        }
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // -------- ACTIVE CHECKS --------
    public async Task<bool> HasActiveAlertForColorAsync(string color, CancellationToken ct = default)
    {
        var p = new SqlParameter("@Color", color);
        return await _db.Database.SqlQueryRaw<bool>("SELECT dbo.fn_HasActiveAlertForColor(@Color)", p).FirstAsync(ct);
    }

    public async Task<bool> HasActiveAlertForMultiAsync(int multiCableId, CancellationToken ct = default)
    {
        return await _db.Alerts.AsNoTracking()
            .AnyAsync(a => a.IsActive && a.AlertType == "Multi" && a.MultiCableID == multiCableId, ct);
    }

    // -------- AUTO-TRIGGER ENTRY POINTS --------
    public async Task<ThresholdEvaluationResult> EvaluateSingleThresholdAsync(
        string color, int currentQty, int minThreshold, CancellationToken ct = default)
    {
        var active = await _db.Alerts
            .FirstOrDefaultAsync(a => a.IsActive && a.AlertType == "Color" && a.Color == color, ct);

        if (currentQty < minThreshold)
        {
            if (active is null)
            {
                var alert = new Alert
                {
                    AlertType = "Color",
                    Color = color,
                    AlertDate = DateTime.Now,
                    Description = Trim255($"Renk={color}, Qty={currentQty}, Min={minThreshold}"),
                    IsActive = true
                };
                _db.Alerts.Add(alert);
                await _db.SaveChangesAsync(ct);

                var rcptCount = await SendLowStockMailForSingleAsync(color, currentQty, minThreshold, ct);
                return new ThresholdEvaluationResult(rcptCount > 0, false, false, rcptCount, currentQty, minThreshold, "Single", color);
            }
            return new ThresholdEvaluationResult(false, false, true, 0, currentQty, minThreshold, "Single", color);
        }
        else
        {
            if (active is not null)
            {
                active.IsActive = false;
                await _db.SaveChangesAsync(ct);
                return new ThresholdEvaluationResult(false, true, true, 0, currentQty, minThreshold, "Single", color);
            }
            return new ThresholdEvaluationResult(false, false, false, 0, currentQty, minThreshold, "Single", color);
        }
    }

    public async Task<ThresholdEvaluationResult> EvaluateMultiThresholdAsync(
        int multiCableId, int currentQty, int minThreshold, CancellationToken ct = default)
    {
        var active = await _db.Alerts
            .FirstOrDefaultAsync(a => a.IsActive && a.AlertType == "Multi" && a.MultiCableID == multiCableId, ct);

        if (currentQty < minThreshold)
        {
            if (active is null)
            {
                var alert = new Alert
                {
                    AlertType = "Multi",
                    MultiCableID = multiCableId,
                    AlertDate = DateTime.Now,
                    Description = Trim255($"MultiCableID={multiCableId}, Qty={currentQty}, Min={minThreshold}"),
                    IsActive = true
                };
                _db.Alerts.Add(alert);
                await _db.SaveChangesAsync(ct);

                var rcptCount = await SendLowStockMailForMultiAsync(multiCableId, currentQty, minThreshold, ct);
                return new ThresholdEvaluationResult(rcptCount > 0, false, false, rcptCount, currentQty, minThreshold, "Multi", multiCableId.ToString());
            }
            return new ThresholdEvaluationResult(false, false, true, 0, currentQty, minThreshold, "Multi", multiCableId.ToString());
        }
        else
        {
            if (active is not null)
            {
                active.IsActive = false;
                await _db.SaveChangesAsync(ct);
                return new ThresholdEvaluationResult(false, true, true, 0, currentQty, minThreshold, "Multi", multiCableId.ToString());
            }
            return new ThresholdEvaluationResult(false, false, false, 0, currentQty, minThreshold, "Multi", multiCableId.ToString());
        }
    }

    // -------- E-MAIL NOTIFICATIONS --------
    public async Task<bool> NotifyAdminsForAlertAsync(int alertId, CancellationToken ct = default)
    {
        var alert = await _db.Alerts.AsNoTracking().FirstOrDefaultAsync(a => a.AlertID == alertId, ct);
        if (alert is null) return false;

        var adminEmails = await GetAdminEmailsAsync(ct);
        if (adminEmails.Count == 0) return false;

        var (subject, html, text) = BuildAlertEmail(alert);
        await _email.SendAsync(adminEmails[0], subject, html, text, bcc: adminEmails.Skip(1), ct: ct);
        return true;
    }

    public async Task<bool> NotifyAdminsForLowStockAsync(string color, int qty, CancellationToken ct = default)
    {
        var adminEmails = await GetAdminEmailsAsync(ct);
        if (adminEmails.Count == 0) return false;

        var (subject, html, text) = BuildSingleLowStockEmail(color, qty, minThreshold: null);
        await _email.SendAsync(adminEmails[0], subject, html, text, bcc: adminEmails.Skip(1), ct: ct);
        return true;
    }

    // -------- PRIVATE HELPERS --------
    private async Task<List<string>> GetAdminEmailsAsync(CancellationToken ct)
    {
        return await _db.Users
            .Where(u => u.Role == AdminRole && u.IsActive && !string.IsNullOrEmpty(u.Email))
            .Select(u => u.Email!)
            .Distinct()
            .ToListAsync(ct);
    }

    private static (string Subject, string Html, string Text) BuildSingleLowStockEmail(string color, int qty, int? minThreshold)
    {
        var subject = $"Stok Uyarısı • {color} kablosu kritik seviyede";
        var minInfo = minThreshold is null ? "" : $" (Min: {minThreshold})";
        var html = $@"<h3>Stok Uyarısı</h3>
<p><b>{color}</b> renkli kablonun stoğu <b>{qty}</b>{minInfo}.</p>
<p>Lütfen gerekli replenishment/giriş işlemlerini başlatın.</p>";
        var text = $"Stok Uyarısı: {color} kablosunun stoğu {qty}{(minThreshold is null ? "" : $" (Min: {minThreshold})")}.";
        return (subject, html, text);
    }

    private static (string Subject, string Html, string Text) BuildMultiLowStockEmail(int multiCableId, int qty, int minThreshold)
    {
        var subject = $"Stok Uyarısı • MultiCable #{multiCableId} kritik seviyede";
        var html = $@"<h3>Stok Uyarısı</h3>
<p><b>MultiCableID: {multiCableId}</b> stoğu <b>{qty}</b> (Min: {minThreshold}).</p>
<p>Lütfen gerekli replenishment/giriş işlemlerini başlatın.</p>";
        var text = $"Stok Uyarısı: MultiCable #{multiCableId} stoğu {qty} (Min: {minThreshold}).";
        return (subject, html, text);
    }

    private static (string Subject, string Html, string Text) BuildAlertEmail(Alert a)
    {
        var subject = a.AlertType switch
        {
            "Color" => $"Stok Uyarısı • {a.Color} rengi",
            "Multi" => $"Stok Uyarısı • MultiCable #{a.MultiCableID}",
            _ => "Stok Uyarısı"
        };

        var html = $@"<h3>Stok Uyarısı</h3>
<ul>
<li><b>Tür:</b> {a.AlertType}</li>
{(string.IsNullOrWhiteSpace(a.Color) ? "" : $"<li><b>Renk:</b> {a.Color}</li>")}
{(a.MultiCableID is null ? "" : $"<li><b>MultiCableID:</b> {a.MultiCableID}</li>")}
<li><b>Tarih:</b> {a.AlertDate:yyyy-MM-dd HH:mm}</li>
{(string.IsNullOrWhiteSpace(a.Description) ? "" : $"<li><b>Açıklama:</b> {a.Description}</li>")}
<li><b>Durum:</b> {(a.IsActive ? "Aktif" : "Kapalı")}</li>
</ul>";

        var text =
            $"Stok Uyarısı\n" +
            $"Tür: {a.AlertType}\n" +
            (string.IsNullOrWhiteSpace(a.Color) ? "" : $"Renk: {a.Color}\n") +
            (a.MultiCableID is null ? "" : $"MultiCableID: {a.MultiCableID}\n") +
            $"Tarih: {a.AlertDate:yyyy-MM-dd HH:mm}\n" +
            (string.IsNullOrWhiteSpace(a.Description) ? "" : $"Açıklama: {a.Description}\n") +
            $"Durum: {(a.IsActive ? "Aktif" : "Kapalı")}";

        return (subject, html, text);
    }

    private static string Trim255(string s) => s.Length > 255 ? s[..255] : s;
     // .net 8 ile gelen [a..b] fonksiyonu:range fonksiyonu a'dan b'ye kadar
    private async Task<int> SendLowStockMailForSingleAsync(string color, int qty, int min, CancellationToken ct)
    {
        var emails = await GetAdminEmailsAsync(ct);
        if (emails.Count == 0) return 0;
        var (subject, html, text) = BuildSingleLowStockEmail(color, qty, min);
        await _email.SendAsync(emails[0], subject, html, text, bcc: emails.Skip(1), ct: ct);
        return emails.Count;
    }

    private async Task<int> SendLowStockMailForMultiAsync(int multiCableId, int qty, int min, CancellationToken ct)
    {
        var emails = await GetAdminEmailsAsync(ct);
        if (emails.Count == 0) return 0;
        var (subject, html, text) = BuildMultiLowStockEmail(multiCableId, qty, min);
        await _email.SendAsync(emails[0], subject, html, text, bcc: emails.Skip(1), ct: ct);
        return emails.Count;
    }
}
