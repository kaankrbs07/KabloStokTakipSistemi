namespace KabloStokTakipSistemi.DTOs.Users;

// Create — Admins + (ayrı gelen CreateUserDto ile Users)
public record CreateAdminDto(
    string Username,
    string DepartmentName,
    long UserID
);

// Read
public record GetAdminDto(
    string Username,
    string DepartmentName,   // Admins tablosundan
    string? FirstName,       // Users tablosundan
    string? LastName         // Users tablosundan
);

// Update — temel alanlar
public record UpdateAdminDto(
    string Username,
    string? DepartmentName,
    long? UserID
);


