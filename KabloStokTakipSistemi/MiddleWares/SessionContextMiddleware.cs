using System.Data;
using System.Security.Claims;
using KabloStokTakipSistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace KabloStokTakipSistemi.Middlewares;

public sealed class SessionContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionContextMiddleware> _logger;

    public SessionContextMiddleware(RequestDelegate next, ILogger<SessionContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        // 1) Actor: login yoksa 0 
        var claim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = long.TryParse(claim, out var uid) ? uid : 0L;

        // 2) CorrelationId
        var correlationId = context.TraceIdentifier ?? Guid.NewGuid().ToString("N");

        var conn = db.Database.GetDbConnection();
        var openedHere = false;

        try
        {
            // Aynı connection'da tut (SESSION_CONTEXT kaybolmasın)
            if (conn.State != ConnectionState.Open)
            {
                await db.Database.OpenConnectionAsync();
                openedHere = true;
            }

            try
            {
                // Fail-safe: hata yerse bile isteği durdurma
                await db.Database.ExecuteSqlRawAsync(
                    "EXEC sp_set_session_context N'UserID', {0}; " +
                    "EXEC sp_set_session_context N'CorrelationId', {1};",
                    userId, correlationId);

                _logger.LogDebug("SESSION_CONTEXT set => UserID={UserID}, CorrelationId={CorrelationId}",
                    userId, correlationId);
            }
            catch (Exception ex)
            {
                // Sadece uyarı logla, pipeline devam etsin
                _logger.LogWarning(ex, "SESSION_CONTEXT set edilemedi (devam ediliyor). UserID={UserID}", userId);
            }

            // İstek tamamlanınca kapat (yalnızca biz açtıysak)
            if (openedHere)
            {
                context.Response.OnCompleted(async () =>
                {
                    try { await db.Database.CloseConnectionAsync(); } catch { /* ignore */ }
                });
            }

            await _next(context);
        }
        finally
        {
            // OnCompleted tetiklenmeden biten edge-case'ler için emniyet
            if (openedHere && conn.State == ConnectionState.Open)
            {
                try { await db.Database.CloseConnectionAsync(); } catch { /* ignore */ }
            }
        }
    }
}
