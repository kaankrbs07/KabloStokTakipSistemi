using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KabloStokTakipSistemi.DTOs;

namespace KabloStokTakipSistemi.Services.Interfaces
{
    public interface ILogService
    {
        Task<PagedResult<LogDto>> GetAsync(LogFilterDto filter, CancellationToken ct = default);
        Task<IReadOnlyList<LogDto>> GetLatestAsync(int take = 50, CancellationToken ct = default);

        // İstatistik (raporlar için)
        Task<IReadOnlyList<LogStatDto>> GetCountByOperationAsync(DateTime? from = null, DateTime? to = null,
            CancellationToken ct = default);

        Task<IReadOnlyList<LogStatDto>> GetCountByTableAsync(DateTime? from = null, DateTime? to = null,
            CancellationToken ct = default);

        // İsteğe bağlı: manuel log (ör. excel ile toplu düzeltme vs.)
        Task<bool> LogManualStockEditAsync(ManualStockEditLogDto dto, CancellationToken ct = default);
    }
}