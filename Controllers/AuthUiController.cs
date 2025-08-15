using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[AllowAnonymous]
[Route("Auth")]
public sealed class AuthUiController : Controller
{
    // GET /Auth/Login -> Views/Auth/Login.cshtml
    [HttpGet("Login")]
    public IActionResult Login() => View("~/Views/Auth/Login.cshtml");

    // GET /Auth/SignUp -> Views/Auth/SignUp.cshtml
    [HttpGet("SignUp")]
    public IActionResult SignUp() => View("~/Views/Auth/SignUp.cshtml");
}