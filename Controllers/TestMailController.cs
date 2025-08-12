using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;
[ApiController]
[Route("api/testmail")]
public sealed class TestMailController : ControllerBase
{
    private readonly IEmailService _email;
    public TestMailController(IEmailService email) => _email = email;

    public sealed record SendMailRequest(string To, string Subject, string Html);

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendMailRequest req)
    {
        await _email.SendAsync(req.To, req.Subject, req.Html);
        return Ok(new { message = "Mail gönderildi." });
    }
    [HttpGet("send-test")]
    public async Task<IActionResult> SendTest()
    {
        await _email.SendAsync("kadirtokagoz6@gmail.com", "Test Mail", "<b>Merhaba, TEST MAİL</b>");
        return Ok(new { message = "Test mail gönderildi." });
    }
}