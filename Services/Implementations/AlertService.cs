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
    private readonly ILogger<AlertService> _log;
    private const string AdminRole = "Admin";

    public AlertService(AppDbContext db, IMapper mapper, IEmailService email, ILogger<AlertService> log)
    {
        _db = db;
        _mapper = mapper;
        _email = email;
        _log = log;
    }

    // -------------------- QUERY METHODS --------------------

    public async Task<IReadOnlyList<GetAlertDto>> GetAlertsAsync(
        bool? isActive = null,
        string? alertType = null,
        string? color = null,
        int? multiCableId = null,
        DateTime? from = null,
        DateTime? to = null,
        int? skip = null,
        int? take = null,
        CancellationToken ct = default)
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

    // -------------------- STATE CHANGE METHODS --------------------

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
        _log.LogInformation("Alert {AlertID} kapatıldı.", alertId);
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
        _log.LogInformation("Alert {AlertID} tekrar aktif edildi.", alertId);
        return true;
    }

    public async Task<bool> HasActiveAlertForColorAsync(string color, CancellationToken ct = default)
    {
        var p = new SqlParameter("@Color", color);

        var result = await _db.Database
            .SqlQueryRaw<bool>("SELECT dbo.fn_HasActiveAlertForColor(@Color)", p)
            .FirstAsync(ct);

        return result;
    }


    // -------------------- EMAIL NOTIFICATIONS --------------------

    /// <summary>
    /// Verilen AlertID için admin kullanıcılara mail atar. Null/boş e-postalar otomatik atlanır.
    /// Tek SMTP çağrısı için BCC kullanılır. Hiç admin yoksa false döner.
    /// </summary>
    public async Task<bool> NotifyAdminsForAlertAsync(int alertId, CancellationToken ct = default)
    {
        var alert = await _db.Alerts.AsNoTracking().FirstOrDefaultAsync(a => a.AlertID == alertId, ct);
        if (alert is null)
        {
            _log.LogWarning("NotifyAdminsForAlertAsync: AlertID {AlertID} bulunamadı.", alertId);
            return false;
        }

        var adminEmails = await _db.Users
            .Where(u => u.Role == AdminRole && u.IsActive && !string.IsNullOrEmpty(u.Email))
            .Select(u => u.Email!)
            .Distinct()
            .ToListAsync(ct);

        if (adminEmails.Count == 0)
        {
            _log.LogInformation("NotifyAdminsForAlertAsync: AlertID {AlertID} için admin e-postası bulunamadı.",
                alertId);
            return false;
        }

        var (subject, html, text) = BuildAlertEmail(alert);

        await _email.SendAsync(
            to: adminEmails[0], // To zorunlu olduğundan ilk adresi To,
            subject: subject,
            htmlBody: html,
            textBody: text,
            bcc: adminEmails.Skip(1),
            ct: ct
        );

        _log.LogInformation("NotifyAdminsForAlertAsync: AlertID {AlertID} için {Count} admin'e e-posta gönderildi.",
            alertId, adminEmails.Count);
        return true;
    }

    /// <summary>
    /// Renk-bazlı kritik stok uyarısı için adminlere mail. (trigger/SP sonrası kullanılabilir)
    /// </summary>
    public async Task<bool> NotifyAdminsForLowStockAsync(string color, int qty, CancellationToken ct = default)
    {
        var adminEmails = await _db.Users
            .Where(u => u.Role == AdminRole && u.IsActive && !string.IsNullOrEmpty(u.Email))
            .Select(u => u.Email!)
            .Distinct()
            .ToListAsync(ct);

        if (adminEmails.Count == 0)
        {
            _log.LogInformation("NotifyAdminsForLowStockAsync: Admin e-postası bulunamadı. Color={Color}, Qty={Qty}",
                color, qty);
            return false;
        }

        var subject = $"Stok Uyarısı • {color} kablosu kritik seviyede";
        var html = $@"
            <h3>Stok Uyarısı</h3>
            <p><b>{color}</b> renkli kablonun stoğu <b>{qty}</b> adede düştü.</p>
            <p>Lütfen gerekli replenishment/giriş işlemlerini başlatın.</p>";
        var text = $"Stok Uyarısı: {color} kablosunun stoğu {qty} adede düştü.";

        await _email.SendAsync(
            to: adminEmails[0],
            subject: subject,
            htmlBody: html,
            textBody: text,
            bcc: adminEmails.Skip(1),
            ct: ct
        );

        _log.LogInformation("NotifyAdminsForLowStockAsync: {Color} rengi için {Count} admin'e e-posta gönderildi.",
            color, adminEmails.Count);
        return true;
    }

    // -------------------- PRIVATE HELPERS --------------------

    private static (string Subject, string Html, string Text) BuildAlertEmail(Alert a)
    {
        var subject = a.AlertType switch
        {
            "Color" => $"Stok Uyarısı • {a.Color} rengi",
            "Multi" => $"Stok Uyarısı • MultiCable #{a.MultiCableID}",
            _ => "Stok Uyarısı"
        };

        var html = $@"
            <h3>Stok Uyarısı</h3>
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
}