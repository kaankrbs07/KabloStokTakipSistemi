using System.Security.Claims;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    // Admin login
    [HttpPost("login/admin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAdmin([FromBody] LoginAdminRequest req, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var token = await _auth.LoginAdminAsync(req, ct);
        if (token is null) return Unauthorized(); // Geçersiz kimlik bilgisi

        return Ok(token);
    }

    // Employee login
    [HttpPost("login/employee")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginEmployee([FromBody] LoginEmployeeRequest req, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var token = await _auth.LoginEmployeeAsync(req, ct);
        if (token is null) return Unauthorized();

        return Ok(token);
    }

    // Employee register (isterseniz bunu Admin'e kapatın/açın)
    [HttpPost("register/employee")]
    [AllowAnonymous] 
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterEmployee([FromBody] RegisterEmployeeRequest req, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var ok = await _auth.RegisterEmployeeAsync(req, ct);
        return ok ? Ok() : BadRequest();
    }

    // Kimlik bilgisi doğrulama / profil (token'dan)
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role   = User.FindFirstValue(ClaimTypes.Role);
        var name   = User.FindFirstValue(ClaimTypes.Name);

        return Ok(new { userId, role, name });
    }
}
