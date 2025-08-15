using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using KabloStokTakipSistemi.Data;

namespace KabloStokTakipSistemi.Middlewares;

public class SessionContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionContextMiddleware> _logger;

    public SessionContextMiddleware(RequestDelegate next, ILogger<SessionContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        // 1) Actor UserID: kimlik varsa claim'den; yoksa 0 (SYSTEM)
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var actorId = long.TryParse(userIdClaim, out var uid) ? uid : 0L;

        // 2) CorrelationId: mevcut traceId ya da yeni guid
        var correlationId = context.TraceIdentifier ?? Guid.NewGuid().ToString("N");

        try
        {
            // İki değeri de SQL oturumuna yaz
            await dbContext.Database.ExecuteSqlRawAsync(
                "EXEC sp_set_session_context N'UserID', {0}; EXEC sp_set_session_context N'CorrelationId', {1};",
                actorId, correlationId);

            _logger.LogInformation("SESSION_CONTEXT set: UserID={UserID}, CorrelationId={CorrelationId}", actorId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SESSION_CONTEXT ayarlanırken hata oluştu. UserID={UserID}", actorId);
        }

        await _next(context);
    }
}