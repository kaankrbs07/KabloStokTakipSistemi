namespace KabloStokTakipSistemi.DTOs;

public record CreateDepartmentDto
{
    public string? DepartmentName { get; init; }
    public long? AdminID { get; init; }   // nullable
}

public record UpdateDepartmentDto
{
    public string? DepartmentName { get; init; }
    public long? AdminID { get; init; }   // nullable
}

public record GetDepartmentDto
{
    public int DepartmentID { get; init; }
    public string? DepartmentName { get; init; }
    public long? AdminID { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsActive { get; init; }
}