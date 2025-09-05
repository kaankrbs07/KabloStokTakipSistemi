using System.ComponentModel.DataAnnotations;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Middlewares;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
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

    // GET: api/users/{id}
    [HttpGet("{id:long}", Name = "GetUserById")]
    public async Task<ActionResult<GetUserDto>> GetById(long id, CancellationToken ct)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return user is null
            ? NotFound(new ErrorBody(AppErrors.Common.NotFound.Code))
            : Ok(user);
    }

    // POST: api/users
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var ok = await _userService.CreateUserAsync(dto);
        if (!ok) return BadRequest(new ErrorBody(AppErrors.Common.Unexpected.Code));

        // Elinde yeni ID varsa kullan; örnekte dto.UserID üzerinden dönüyor
        return CreatedAtRoute("GetUserById", new { id = dto.UserID }, new { id = dto.UserID });
    }

    // PATCH: api/users/{id}/status 
    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        // route -> dto eşlemesi
        //Body'den gelen dto nesnesini al, ama onun UserID alanını URL'den gelen id değeriyle değiştir
        //Yani body'de ne yazarsa yazsın, biz URL'deki ID'yi kullanıyoruz. Bu sayede hangi kullanıcının güncelleneceği konusunda bir karışıklık olmuyor.
        dto = dto with { UserID = id };

        var ok = await _userService.UpdateUserAsync(dto);
        return ok ? NoContent() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }

    // PATCH: api/users/{id}  (soft-delete / pasif etme)
    [HttpPatch("{id:long}")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        var ok = await _userService.DeactivateUserAsync(id);
        return ok ? NoContent() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }
}