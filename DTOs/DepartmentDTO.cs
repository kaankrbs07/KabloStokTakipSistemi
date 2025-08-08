namespace KabloStokTakipSistemi.DTOs;

public record CreateDepartmentDto(
    string? DepartmentName,
    long AdminID
);

public record GetDepartmentDto(
    int DepartmentID,
    string? DepartmentName,
    DateTime CreatedAt,
    string AdminUsername
);


public record UpdateDepartmentDto(
    int DepartmentID,
    string? DepartmentName,
    long? AdminID
);
