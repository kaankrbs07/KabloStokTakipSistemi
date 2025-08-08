
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetUserById(long id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.CreateUserAsync(dto);
        if (!result)
            return BadRequest(new { message = "Kullanıcı oluşturulamadı." });

        return Ok(new { message = "Kullanıcı başarıyla oluşturuldu." });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        dto = dto with { UserID = id }; // DTO içindeki UserID'yi route parametresi ile eşleştir

        var result = await _userService.UpdateUserAsync(dto);
        if (!result)
            return NotFound(new { message = "Güncelleme başarısız. Kullanıcı bulunamadı." });

        return Ok(new { message = "Kullanıcı güncellendi." });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeactivateUser(long id)
    {
        var result = await _userService.DeactivateUserAsync(id);
        if (!result)
            return NotFound(new { message = "Kullanıcı pasif hâle getirilemedi. Kullanıcı bulunamadı." });

        return Ok(new { message = "Kullanıcı başarıyla pasif hale getirildi." });
    }

    [HttpGet("{id:long}/summary")]
    public async Task<IActionResult> GetUserActivitySummary(long id)
    {
        var summary = await _userService.GetUserActivitySummaryAsync(id);
        if (summary == null)
            return NotFound(new { message = "Kullanıcının etkinlik özeti bulunamadı." });

        return Ok(summary);
    }
}
