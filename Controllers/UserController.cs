using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetUserDto>>> GetAll(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting all users");
            var users = await _userService.GetAllUsersAsync();
            _logger.LogInformation("Retrieved {Count} users", users.Count());
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            throw;
        }
    }

    // GET: api/users/id
    [HttpGet("{id:long}", Name = "GetUserById")]
    public async Task<ActionResult<GetUserDto>> GetById(long id, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting user with ID: {UserId}", id);
            var user = await _userService.GetUserByIdAsync(id);

            if (user is null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", id);
                return NotFound(new { message = "Kullanıcı bulunamadı." });
            }

            _logger.LogInformation("Retrieved user: {UserId}", id);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user with ID: {UserId}", id);
            throw;
        }
    }

    // POST: api/users
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Creating new user with ID: {UserId}", dto.UserID);
            var ok = await _userService.CreateUserAsync(dto);

            if (!ok)
            {
                _logger.LogWarning("Failed to create user with ID: {UserId}", dto.UserID);
                return BadRequest(new { message = "Kullanıcı oluşturulamadı." });
            }

            _logger.LogInformation("Successfully created user with ID: {UserId}", dto.UserID);
            return CreatedAtRoute("GetUserById", new { id = dto.UserID },
                new { message = "Kullanıcı oluşturuldu.", id = dto.UserID });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with ID: {UserId}", dto.UserID);
            throw;
        }
    }

    // PUT: api/users/id
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Updating user with ID: {UserId}", id);
            // route -> dto eşlemesi
            dto = dto with { UserID = id };

            var ok = await _userService.UpdateUserAsync(dto);
            if (!ok)
            {
                _logger.LogWarning("Failed to update user with ID: {UserId}", id);
                return NotFound(new { message = "Güncelleme başarısız. Kullanıcı bulunamadı." });
            }

            _logger.LogInformation("Successfully updated user with ID: {UserId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID: {UserId}", id);
            throw;
        }
    }

    // DELETE: api/users/id  (soft-delete / pasif etme)
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Deactivating user with ID: {UserId}", id);
            var ok = await _userService.DeactivateUserAsync(id);

            if (!ok)
            {
                _logger.LogWarning("Failed to deactivate user with ID: {UserId}", id);
                return NotFound(new { message = "Kullanıcı pasif hâle getirilemedi. Kullanıcı bulunamadı." });
            }

            _logger.LogInformation("Successfully deactivated user with ID: {UserId}", id);
            return Ok(new { message = "Kullanıcı pasif hâle getirildi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user with ID: {UserId}", id);
            throw;
        }
    }

    // GET: api/users/id/summary
    [HttpGet("{id:long}/summary")]
    public async Task<IActionResult> GetActivitySummary(long id, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting activity summary for user ID: {UserId}", id);
            var summary = await _userService.GetUserActivitySummaryAsync(id);

            if (summary is null)
            {
                _logger.LogWarning("Activity summary not found for user ID: {UserId}", id);
                return NotFound(new { message = "Etkinlik özeti bulunamadı." });
            }

            _logger.LogInformation("Retrieved activity summary for user ID: {UserId}", id);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity summary for user ID: {UserId}", id);
            throw;
        }
    }
}