using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();
}
