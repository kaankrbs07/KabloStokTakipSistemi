using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    /// Token üzerinden oturum açmış kullanıcının ID’sini çeker.
    /// Eğer yoksa -1 döner (JWT boşsa veya devrede değilse).
    protected long CurrentUserId =>
        long.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : -1;
}