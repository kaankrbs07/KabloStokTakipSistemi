using System.ComponentModel.DataAnnotations;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Middlewares;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    // GET: api/employee
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetEmployeeDto>>> GetAll(CancellationToken ct)
    {
        var employees = await _employeeService.GetAllEmployeesAsync();
        return Ok(employees);
    }

    // GET: api/employee/{id}
    [HttpGet("{id:long}", Name = "GetEmployeeById")]
    public async Task<ActionResult<GetEmployeeDto>> GetById(long id, CancellationToken ct)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        return employee is null
            ? NotFound(new ErrorBody(AppErrors.Common.NotFound.Code))
            : Ok(employee);
    }

    // POST: api/employee
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto, CancellationToken ct)
    {
        var ok = await _employeeService.CreateEmployeeAsync(dto);
        if (!ok)
            return BadRequest(new ErrorBody(AppErrors.Common.Unexpected.Code));

        return CreatedAtRoute("GetEmployeeById", new { id = dto.EmployeeID }, new { id = dto.EmployeeID });
    }

    // PATCH: api/employee/{id}/department
    [HttpPatch("{id:long}/department")]
    public async Task<IActionResult> UpdateDepartment(long id, [FromBody] UpdateEmployeeDepartmentRequest body, CancellationToken ct)
    {
        var ok = await _employeeService.UpdateEmployeeDepartmentAsync(id, body.DepartmentId);
        return ok ? NoContent() : NotFound(new ErrorBody(AppErrors.Common.NotFound.Code));
    }

    public sealed record UpdateEmployeeDepartmentRequest([property: Required] int DepartmentId);
}
