using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    public UsersController(IUserService userService) => _userService = userService;

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetUserDto>>> GetAll(CancellationToken ct)
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    // GET: api/users/id
    [HttpGet("{id:long}", Name = "GetUserById")]
    public async Task<ActionResult<GetUserDto>> GetById(long id, CancellationToken ct)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return user is null ? NotFound(new { message = "Kullanıcı bulunamadı." }) : Ok(user);
    }

    // POST: api/users
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var ok = await _userService.CreateUserAsync(dto);
        if (!ok) return BadRequest(new { message = "Kullanıcı oluşturulamadı." });

        // dto.UserID sizde dışarıdan geliyor (numeric(10,0)); CreatedAtAction ile Location header verelim
        return CreatedAtRoute("GetUserById", new { id = dto.UserID }, new { message = "Kullanıcı oluşturuldu.", id = dto.UserID });
    }

    // PUT: api/users/id
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        // route -> dto eşlemesi
        dto = dto with { UserID = id };

        var ok = await _userService.UpdateUserAsync(dto);
        if (!ok) return NotFound(new { message = "Güncelleme başarısız. Kullanıcı bulunamadı." });

        return NoContent();
    }

    // DELETE: api/users/id  (soft-delete / pasif etme)
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        var ok = await _userService.DeactivateUserAsync(id);
        if (!ok) return NotFound(new { message = "Kullanıcı pasif hâle getirilemedi. Kullanıcı bulunamadı." });

        return Ok(new { message = "Kullanıcı pasif hâle getirildi." });
    }

    // GET: api/users/id/summary
    [HttpGet("{id:long}/summary")]
    public async Task<IActionResult> GetActivitySummary(long id, CancellationToken ct)
    {
        var summary = await _userService.GetUserActivitySummaryAsync(id);
        return summary is null ? NotFound(new { message = "Etkinlik özeti bulunamadı." }) : Ok(summary);
    }
}

