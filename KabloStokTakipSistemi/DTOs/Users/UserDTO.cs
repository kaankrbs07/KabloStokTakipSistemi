
namespace KabloStokTakipSistemi.DTOs.Users;

public sealed record CreateUserDto(
    long UserID,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    int? DepartmentID,
    string Role,           // "Admin" | "Employee"
    bool IsActive,
    string Password,       // Users tablosunda
    string? AdminUsername, // only if Role == "Admin"
    string? AdminDepartmentName // only if Role == "Admin"
);

public record GetUserDto(
    long UserID,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    string? DepartmentName // join ile Departments'tan okunur
);

public record UpdateUserDto(
    long UserID,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    int? DepartmentID,
    string? Role,
    bool? IsActive,
    string? Password
);