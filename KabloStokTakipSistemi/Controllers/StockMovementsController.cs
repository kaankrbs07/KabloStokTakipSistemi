using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ClosedXML.Excel;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Middlewares;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // import işlemi sadece Admin
public sealed class StockMovementsController : ControllerBase
{
    private static readonly HashSet<string> AllowedTables =
        new(StringComparer.OrdinalIgnoreCase) { "Single", "Multi" };

    private static readonly HashSet<string> AllowedMovements =
        new(StringComparer.OrdinalIgnoreCase) { "Giriş", "Çıkış" };

    private readonly IStockMovementService _service;
    public StockMovementsController(IStockMovementService service) => _service = service;

    // POST api/stockmovements
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Insert([FromBody] CreateStockMovementDto dto, CancellationToken ct)
    {
        if (!AllowedTables.Contains(dto.TableName) || !AllowedMovements.Contains(dto.MovementType))
            return BadRequest(new ErrorBody(AppErrors.Validation.BadRequest.Code));

        await _service.InsertStockMovementAsync(dto);
        return Ok();
    }

    // GET api/stockmovements/history
    [HttpGet("history")]
    [AllowAnonymous] // gerekirse aç/kapatsın
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GetStockMovementDto>>> GetHistory(CancellationToken ct)
    {
        var data = await _service.GetHistoryAsync();
        return Ok(data);
    }

    // GET api/stockmovements/history/filter
    [HttpGet("history/filter")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            return BadRequest(new ErrorBody(AppErrors.Validation.BadRequest.Code));

        var filter = new StockMovementFilterDto(tableName, cableName, color, userId, dateFrom, dateTo);
        var data = await _service.GetHistoryFilteredAsync(filter);
        return Ok(data);
    }

    // ================== EXCEL IMPORT ==================

    /// Excel (.xlsx/.xls/.csv) dosyasını içe aktarır. Varsayılan: dryRun = true
    /// Form-data: file=IFormFile, dryRun=true|false
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Import([Required] IFormFile file, [FromForm] bool? dryRun, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ErrorBody(AppErrors.Validation.BadRequest.Code));

        var isDry = dryRun ?? true;

        // Sadece Excel/CSV uzantıları
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".xlsx" and not ".xls" and not ".csv")
            return BadRequest(new ErrorBody(AppErrors.Validation.BadRequest.Code));

        // JWT’den UserID (SessionContextMiddleware zaten NameIdentifier kullanıyor)
        var uidClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(uidClaim))
            return Unauthorized(new ErrorBody(AppErrors.Common.Unauthorized.Code));

        if (!decimal.TryParse(uidClaim, out var userId))
            return Unauthorized(new ErrorBody(AppErrors.Common.Unauthorized.Code));

        // Şimdilik sadece .xlsx/.xls işleyelim; CSV’yi servis katmanına ekleyebilirsin
        if (ext == ".csv")
            return BadRequest(new ErrorBody(AppErrors.Validation.BadRequest.Code));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;

        // Servisin ImportFromExcelAsync metodunu çağırıyoruz
        var result = await _service.ImportFromExcelAsync(ms, isDry, userId, ct);

        return Ok(result);
    }


    // İçe aktarım için Excel şablonu üretir ve indirir. 
    // Hazır şablonu kullanmak için 
    [HttpGet("template")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Template()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("ImportTemplate");

        var headers = new[] { "TableName", "MovementType", "Quantity", "Color", "CableID" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Column(i + 1).Width = Math.Max(14, headers[i].Length + 2);
        }

        /* örnek satırlar
        ws.Cell(2, 1).Value = "Single";
        ws.Cell(2, 2).Value = "Giriş";
        ws.Cell(2, 3).Value = 5;
        ws.Cell(2, 4).Value = "Kırmızı";
        ws.Cell(2, 5).Value = ""; // Single için boş

        ws.Cell(3, 1).Value = "Multi";
        ws.Cell(3, 2).Value = "Çıkış";
        ws.Cell(3, 3).Value = 3;
        ws.Cell(3, 4).Value = ""; // Multi için boş
        ws.Cell(3, 5).Value = 42; // MultiCableID
        */

        // Sadece başlık satırını dondur
        ws.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"KSTS_BulkStockImport_Template_{DateTime.Now:yyyyMMddHHmm}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}