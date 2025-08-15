using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

public sealed class HomeController : Controller
{
    // GET / veya /Home/Index  -> Views/Home/Index.cshtml
    public IActionResult Index() => View();
}