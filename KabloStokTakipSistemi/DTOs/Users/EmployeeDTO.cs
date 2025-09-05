namespace KabloStokTakipSistemi.DTOs.Users;

// CREATE — Users + Employees kayıtları için
public record CreateEmployeeDto(
    long EmployeeID,      // Employees PK (numeric(5,0))
    long UserID,          // Users FK (numeric(10,0))
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    int? DepartmentID,
    string Password       // Users.Password
);

// READ — Ekranda göstereceğin özet
public record GetEmployeeDto(
    long EmployeeID,
    string? FirstName,
    string? LastName,
    string? DepartmentName
);

// UPDATE — İlişki değişimi vb. basit senaryolar
public record UpdateEmployeeDto(
    long EmployeeID,
    long? UserID
);