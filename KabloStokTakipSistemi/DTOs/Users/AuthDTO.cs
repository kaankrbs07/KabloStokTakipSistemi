namespace KabloStokTakipSistemi.DTOs.Users;

// Login
public sealed record LoginAdminRequest(string Username, string Password);
public sealed record LoginEmployeeRequest(long EmployeeID, string Password);

// Register (Employee için self-service; Admin kayıtları Admin panelinden)
public sealed record RegisterEmployeeRequest(
    long   UserID,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    int?    DepartmentID,
    string  Password // düz gelir, servis hashler
);

// Token cevabı
public sealed record TokenResponse(
    string AccessToken,
    DateTime ExpiresAt,
    long UserID,
    string Role,
    string? FullName
);