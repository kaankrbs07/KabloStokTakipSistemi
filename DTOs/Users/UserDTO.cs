namespace KabloStokTakipSistemi.DTOs.Users;

public record CreateUserDto(
    long UserID,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string Password,
    string Role,
    bool IsActive,
    int? DepartmentID
);

public record GetUserDto(
    long UserID,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    string? DepartmentName
);

public record UpdateUserDto(
    long UserID,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? Password,
    string? Role,
    bool? IsActive,
    int? DepartmentID
);
