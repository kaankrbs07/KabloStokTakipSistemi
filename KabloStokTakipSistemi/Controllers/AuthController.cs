using System.Security.Claims;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KabloStokTakipSistemi.Middlewares;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    // ----- Admin login -----
    [HttpPost("login/admin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorBody), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorBody), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAdmin([FromBody] LoginAdminRequest req, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            throw new AppException(AppErrors.Validation.BadRequest, "Geçersiz istek gövdesi (ModelState).");

        var token = await _auth.LoginAdminAsync(req, ct);
        if (token is null)
            throw new AppException(AppErrors.Common.Unauthorized, "Geçersiz kullanıcı adı veya şifre.");

        return Ok(token);
    }

    // ----- Employee login -----
    [HttpPost("login/employee")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorBody), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorBody), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginEmployee([FromBody] LoginEmployeeRequest req, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            throw new AppException(AppErrors.Validation.BadRequest, "Geçersiz istek gövdesi (ModelState).");

        var token = await _auth.LoginEmployeeAsync(req, ct);
        if (token is null)
            throw new AppException(AppErrors.Common.Unauthorized, "Geçersiz kimlik bilgileri.");

        return Ok(token);
    }
    

    // ----- Kimlikten profil -----
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorBody), StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role   = User.FindFirstValue(ClaimTypes.Role);
        var name   = User.FindFirstValue(ClaimTypes.Name);

        return Ok(new { userId, role, name });
    }
}
