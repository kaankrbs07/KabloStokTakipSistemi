namespace KabloStokTakipSistemi.DTOs.Users;

public record CreateEmployeeDto(
    long EmployeeID,
    long UserID
);

public record GetEmployeeDto(
    long EmployeeID,
    string? FirstName,
    string? LastName,
    string? DepartmentName
);


public record UpdateEmployeeDto(
    long EmployeeID,
    long? UserID
);
