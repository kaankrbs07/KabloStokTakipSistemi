// Services/LogService.cs
using KabloStokTakipSistemi.Data;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Services.Implementations
{
    public sealed class LogService : ILogService
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public LogService(AppDbContext db, IMapper mapper /* ILogger<LogService> _ opsiyonel */)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<LogDto>> GetAsync(LogFilterDto filter, CancellationToken ct = default)
        {
            IQueryable<Log> q = _db.Set<Log>().AsNoTracking();

            if (filter.FromDate.HasValue)
                q = q.Where(x => x.LogDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                q = q.Where(x => x.LogDate <= filter.ToDate.Value);

            if (filter.UserID.HasValue)
                q = q.Where(x => x.UserID == filter.UserID.Value);

            if (!string.IsNullOrWhiteSpace(filter.TableName))
                q = q.Where(x => x.TableName == filter.TableName);

            if (!string.IsNullOrWhiteSpace(filter.Operation))
                q = q.Where(x => x.Operation == filter.Operation);

            if (!string.IsNullOrWhiteSpace(filter.Search))
                q = q.Where(x => x.Description != null && x.Description.Contains(filter.Search!));

            q = filter.Desc
                ? q.OrderByDescending(x => x.LogDate).ThenByDescending(x => x.LogID)
                : q.OrderBy(x => x.LogDate).ThenBy(x => x.LogID);

            var total = await q.CountAsync(ct);

            int page = Math.Max(1, filter.Page);
            int pageSize = Math.Max(1, filter.PageSize);
            int skip = (page - 1) * pageSize;

            var items = await q.Skip(skip)
                .Take(pageSize)
                .ProjectTo<LogDto>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);

            return new PagedResult<LogDto>(items, total, page, pageSize);
        }

        public async Task<IReadOnlyList<LogDto>> GetLatestAsync(int take = 50, CancellationToken ct = default)
        {
            take = take <= 0 ? 50 : take;

            var items = await _db.Set<Log>()
                .AsNoTracking()
                .OrderByDescending(x => x.LogDate)
                .ThenByDescending(x => x.LogID)
                .Take(take)
                .ProjectTo<LogDto>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);

            return items;
        }

        public async Task<IReadOnlyList<LogStatDto>> GetCountByOperationAsync(
            DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
        {
            IQueryable<Log> q = _db.Set<Log>().AsNoTracking();
            if (from.HasValue) q = q.Where(x => x.LogDate >= from.Value);
            if (to.HasValue)   q = q.Where(x => x.LogDate <= to.Value);

            return await q.GroupBy(x => x.Operation)
                .Select(g => new LogStatDto(g.Key ?? string.Empty, g.Count()))
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<LogStatDto>> GetCountByTableAsync(
            DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
        {
            IQueryable<Log> q = _db.Set<Log>().AsNoTracking();
            if (from.HasValue) q = q.Where(x => x.LogDate >= from.Value);
            if (to.HasValue)   q = q.Where(x => x.LogDate <= to.Value);

            return await q.GroupBy(x => x.TableName)
                .Select(g => new LogStatDto(g.Key ?? string.Empty, g.Count()))
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);
        }

        public async Task<bool> LogManualStockEditAsync(ManualStockEditLogDto dto, CancellationToken ct = default)
        {
            var p = new[]
            {
                new SqlParameter("@CableID", dto.CableID),
                new SqlParameter("@TableName", dto.TableName),
                new SqlParameter("@OldQuantity", dto.OldQuantity),
                new SqlParameter("@NewQuantity", dto.NewQuantity),
                new SqlParameter("@EditedByUserID", dto.EditedByUserID),
                new SqlParameter("@Reason", dto.Reason ?? string.Empty),
            };

            var rows = await _db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_LogManualStockEdit @CableID, @TableName, @OldQuantity, @NewQuantity, @EditedByUserID, @Reason",
                p, ct);

            return rows >= 0;
        }
    }
}
