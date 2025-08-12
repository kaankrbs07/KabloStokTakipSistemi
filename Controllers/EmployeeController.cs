using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KabloStokTakipSistemi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeController> _logger;
    
    public EmployeeController(IEmployeeService employeeService, ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    // GET: api/employee
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetEmployeeDto>>> GetAll(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting all employees");
            var employees = await _employeeService.GetAllEmployeesAsync();
            _logger.LogInformation("Retrieved {Count} employees", employees.Count());
            return Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all employees");
            throw;
        }
    }

    // GET: api/employee/id
    [HttpGet("{id:long}", Name = "GetEmployeeById")]
    public async Task<ActionResult<GetEmployeeDto>> GetById(long id, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting employee with ID: {EmployeeId}", id);
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            
            if (employee is null)
            {
                _logger.LogWarning("Employee not found with ID: {EmployeeId}", id);
                return NotFound(new { message = "Çalışan bulunamadı." });
            }
            
            _logger.LogInformation("Retrieved employee: {EmployeeId}", id);
            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee with ID: {EmployeeId}", id);
            throw;
        }
    }

    // POST: api/employee
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Creating new employee with ID: {EmployeeId}", dto.EmployeeID);
            var ok = await _employeeService.CreateEmployeeAsync(dto);
            
            if (!ok)
            {
                _logger.LogWarning("Failed to create employee with ID: {EmployeeId}", dto.EmployeeID);
                return BadRequest(new { message = "Çalışan oluşturulamadı." });
            }

            _logger.LogInformation("Successfully created employee with ID: {EmployeeId}", dto.EmployeeID);
            return CreatedAtRoute("GetEmployeeById", new { id = dto.EmployeeID }, new { message = "Çalışan oluşturuldu.", id = dto.EmployeeID });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee with ID: {EmployeeId}", dto.EmployeeID);
            throw;
        }
    }

    // PUT: api/employee/id/department
    [HttpPut("{id:long}/department")]
    public async Task<IActionResult> UpdateDepartment(long id, [FromBody] int departmentId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Updating department for employee ID: {EmployeeId} to department ID: {DepartmentId}", id, departmentId);
            var ok = await _employeeService.UpdateEmployeeDepartmentAsync(id, departmentId);
            
            if (!ok)
            {
                _logger.LogWarning("Failed to update department for employee ID: {EmployeeId}", id);
                return NotFound(new { message = "Departman güncellemesi başarısız. Çalışan bulunamadı." });
            }

            _logger.LogInformation("Successfully updated department for employee ID: {EmployeeId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department for employee ID: {EmployeeId}", id);
            throw;
        }
    }
}
