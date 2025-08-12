using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    
    public EmployeeController(IEmployeeService employeeService) => _employeeService = employeeService;

    // GET: api/employee
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetEmployeeDto>>> GetAll(CancellationToken ct)
    {
        var employees = await _employeeService.GetAllEmployeesAsync();
        return Ok(employees);
    }

    // GET: api/employee/id
    [HttpGet("{id:long}", Name = "GetEmployeeById")]
    public async Task<ActionResult<GetEmployeeDto>> GetById(long id, CancellationToken ct)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        return employee is null ? NotFound(new { message = "Çalışan bulunamadı." }) : Ok(employee);
    }

    // POST: api/employee
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto, CancellationToken ct)
    {
        var ok = await _employeeService.CreateEmployeeAsync(dto);
        if (!ok) return BadRequest(new { message = "Çalışan oluşturulamadı." });

        return CreatedAtRoute("GetEmployeeById", new { id = dto.EmployeeID }, new { message = "Çalışan oluşturuldu.", id = dto.EmployeeID });
    }

    // PUT: api/employee/id/department
    [HttpPut("{id:long}/department")]
    public async Task<IActionResult> UpdateDepartment(long id, [FromBody] int departmentId, CancellationToken ct)
    {
        var ok = await _employeeService.UpdateEmployeeDepartmentAsync(id, departmentId);
        if (!ok) return NotFound(new { message = "Departman güncellemesi başarısız. Çalışan bulunamadı." });

        return NoContent();
    }
}
