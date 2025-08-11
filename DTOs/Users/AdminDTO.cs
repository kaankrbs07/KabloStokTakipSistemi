
namespace KabloStokTakipSistemi.DTOs.Users;

// Create — Admins + (ayrı gelen CreateUserDto ile Users)
public record CreateAdminDto(
    long AdminID,
    long UserID,
    string Username,
    string DepartmentName
);

// Read
public record GetAdminDto(
    long AdminID,
    string Username,
    string DepartmentName,   // Admins tablosundan
    string? FirstName,       // Users tablosundan
    string? LastName         // Users tablosundan
);

// Update — temel alanlar
public record UpdateAdminDto(
    long AdminID,
    string? Username,
    string? DepartmentName,
    long? UserID
);


