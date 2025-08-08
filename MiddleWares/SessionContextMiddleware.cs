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
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
        {
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC sp_set_session_context N'UserID', {0}", userId);

                _logger.LogInformation("SESSION_CONTEXT set: UserID = {UserID}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SESSION_CONTEXT ayarlanırken hata oluştu. UserID = {UserID}", userId);
            }
        }
        else
        {
            _logger.LogWarning("SESSION_CONTEXT ayarlanamadı. Geçerli UserID bulunamadı.");
        }

        await _next(context);
    }
}