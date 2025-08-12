
namespace KabloStokTakipSistemi.DTOs;

public record CreateDepartmentDto
{
    public string DepartmentName { get; init; } = null!;
    public int AdminID { get; init; }               // DB: numeric(5,0) -> int uyumlu
}

public record UpdateDepartmentDto
{
    public string DepartmentName { get; init; } = null!;
    public int AdminID { get; init; }
}

public record GetDepartmentDto
{
    public int DepartmentID { get; init; }
    public string? DepartmentName { get; init; }
    public int AdminID { get; init; }
    public DateTime CreatedAt { get; init; }
    
    public bool IsActive { get; set; }
}

