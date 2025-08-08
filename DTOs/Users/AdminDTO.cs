namespace KabloStokTakipSistemi.DTOs.Users;

public record CreateAdminDto(
    long AdminID,
    string Username,
    long UserID
);

public record GetAdminDto(
    long AdminID,
    string Username,
    string? FirstName,
    string? LastName,
    string? DepartmentName
);


public record UpdateAdminDto(
    long AdminID,
    string? Username,
    long? UserID
);
