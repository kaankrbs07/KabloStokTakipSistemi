using AutoMapper;
using AutoMapper.QueryableExtensions;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations;

public sealed class AlertService : IAlertService
{
    private readonly AppDbContext _db; // senin DbContext adını kullan
    private readonly IMapper _mapper;

    public AlertService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

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

        if (isActive is not null)
            q = q.Where(a => a.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(alertType))
            q = q.Where(a => a.AlertType == alertType);

        if (!string.IsNullOrWhiteSpace(color))
            q = q.Where(a => a.Color == color);

        if (multiCableId is not null)
            q = q.Where(a => a.MultiCableID == multiCableId);

        if (from is not null)
            q = q.Where(a => a.AlertDate >= from);

        if (to is not null)
            q = q.Where(a => a.AlertDate <= to);

        q = q.OrderByDescending(a => a.AlertDate).ThenBy(a => a.AlertID);

        if (skip is not null) q = q.Skip(skip.Value);
        if (take is not null) q = q.Take(take.Value);

        return await q.ProjectTo<GetAlertDto>(_mapper.ConfigurationProvider)
                      .ToListAsync(ct);
    }

    public async Task<GetAlertDto?> GetByIdAsync(int alertId, CancellationToken ct = default)
    {
        return await _db.Alerts.AsNoTracking()
                 .Where(a => a.AlertID == alertId)
                 .ProjectTo<GetAlertDto>(_mapper.ConfigurationProvider)
                 .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> ResolveAsync(int alertId, string? resolveNote = null, CancellationToken ct = default)
    {
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.AlertID == alertId, ct);
        if (alert is null) return false;
        if (!alert.IsActive) return true; // zaten kapalı

        alert.IsActive = false;

        if (!string.IsNullOrWhiteSpace(resolveNote))
        {
            var suffix = $" [KAPATILDI: {DateTime.Now:yyyy-MM-dd HH:mm}; Not: {resolveNote}]";
            // 255 sınırına saygı (modelde MaxLength(255))
            var target = (alert.Description ?? string.Empty) + suffix;
            alert.Description = target.Length > 255 ? target[..255] : target;
        }

        // Not: Alert güncellemesi için trigger’ların Log’a yazması beklendiği için burada UPDATE yeterli
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReactivateAsync(int alertId, string? reason = null, CancellationToken ct = default)
    {
        var alert = await _db.Alerts.FirstOrDefaultAsync(a => a.AlertID == alertId, ct);
        if (alert is null) return false;
        if (alert.IsActive) return true; // zaten açık

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

    public async Task<bool> HasActiveAlertForColorAsync(string color, CancellationToken ct = default)
    {
        // fn_HasActiveAlertForColor(@Color) -> BIT
        // EF Core 8: SqlQueryRaw<T> kullanılabilir
        var result = await _db.Database
            .SqlQueryRaw<int>("SELECT dbo.fn_HasActiveAlertForColor(@p0)", parameters: [color])
            .FirstAsync(ct);
        return result == 1;
    }
}
