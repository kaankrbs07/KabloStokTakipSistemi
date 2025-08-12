using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockMovementsController : ControllerBase
{
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase) { "Single", "Multi" };

    private static readonly HashSet<string> AllowedMovements = new(StringComparer.OrdinalIgnoreCase)
        { "Giriş", "Çıkış" };

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
        try
        {
            _logger.LogInformation(
                "Creating stock movement - TableName: {TableName}, MovementType: {MovementType}, Quantity: {Quantity}",
                dto.TableName, dto.MovementType, dto.Quantity);

            if (!AllowedTables.Contains(dto.TableName))
            {
                _logger.LogWarning("Invalid TableName provided: {TableName}", dto.TableName);
                return BadRequest("TableName sadece 'Single' veya 'Multi' olabilir.");
            }

            if (!AllowedMovements.Contains(dto.MovementType))
            {
                _logger.LogWarning("Invalid MovementType provided: {MovementType}", dto.MovementType);
                return BadRequest("MovementType sadece 'Giriş' veya 'Çıkış' olabilir.");
            }

            await _service.InsertAsync(dto);
            _logger.LogInformation("Successfully created stock movement");
            return Created(string.Empty, new { message = "Stok hareketi eklendi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock movement");
            throw;
        }
    }

    // GET api/stockmovements/history
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<GetStockMovementDto>>> GetHistory(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting stock movement history");
            var data = await _service.GetHistoryAsync();
            _logger.LogInformation("Retrieved {Count} stock movements", data.Count());
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock movement history");
            throw;
        }
    }

    // Single için color, Multi için cableNamE
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
        try
        {
            _logger.LogInformation(
                "Getting filtered stock movement history - TableName: {TableName}, CableName: {CableName}, Color: {Color}, UserId: {UserId}",
                tableName, cableName, color, userId);

            if (tableName is not null && !AllowedTables.Contains(tableName))
            {
                _logger.LogWarning("Invalid TableName provided in filter: {TableName}", tableName);
                return BadRequest("TableName sadece 'Single' veya 'Multi' olabilir.");
            }

            var filter = new StockMovementFilterDto(tableName, cableName, color, userId, dateFrom, dateTo);
            var data = await _service.GetHistoryFilteredAsync(filter);
            _logger.LogInformation("Retrieved {Count} filtered stock movements", data.Count());
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered stock movement history");
            throw;
        }
    }
}