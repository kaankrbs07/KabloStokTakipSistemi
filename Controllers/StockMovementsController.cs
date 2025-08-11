using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockMovementsController : ControllerBase
{
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase) { "Single", "Multi" };
    private static readonly HashSet<string> AllowedMovements = new(StringComparer.OrdinalIgnoreCase) { "Giriş", "Çıkış" };

    private readonly IStockMovementService _service;
    private readonly ILogger<StockMovementsController> _logger;

    public StockMovementsController(IStockMovementService service, ILogger<StockMovementsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // POST api/stockmovements
    [HttpPost]
    public async Task<IActionResult> Insert([FromBody] CreateStockMovementDto dto, CancellationToken ct)
    {
        if (!AllowedTables.Contains(dto.TableName))
            return BadRequest("TableName sadece 'Single' veya 'Multi' olabilir.");

        if (!AllowedMovements.Contains(dto.MovementType))
            return BadRequest("MovementType sadece 'Giriş' veya 'Çıkış' olabilir.");

        await _service.InsertAsync(dto);
        return Created(string.Empty, new { message = "Stok hareketi eklendi." });
    }

    // GET api/stockmovements/history
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<GetStockMovementDto>>> GetHistory(CancellationToken ct)
    {
        var data = await _service.GetHistoryAsync();
        return Ok(data);
    }

    // GET api/stockmovements/history/filter?tableName=Multi&cableName=A12&userId=123&dateFrom=2025-08-01&dateTo=2025-08-11
    // Single için color=... kullan; Multi için cableName=... kullan.
    [HttpGet("history/filter")]
    public async Task<ActionResult<IEnumerable<GetStockMovementDto>>> GetHistoryFiltered(
        [FromQuery] string? tableName,
        [FromQuery] string? cableName,
        [FromQuery] string? color,
        [FromQuery] long? userId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct)
    {
        if (tableName is not null && !AllowedTables.Contains(tableName))
            return BadRequest("TableName sadece 'Single' veya 'Multi' olabilir.");

        var filter = new StockMovementFilterDto(tableName, cableName, color, userId, dateFrom, dateTo);
        var data = await _service.GetHistoryFilteredAsync(filter);
        return Ok(data);
    }
}
