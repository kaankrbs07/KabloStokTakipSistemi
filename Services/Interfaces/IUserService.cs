using KabloStokTakipSistemi.DTOs.Users;

namespace KabloStokTakipSistemi.Services.Interfaces;

public interface IUserService
{
    /// Tüm kullanıcıları getirir (aktif/pasif fark etmez).
    Task<IEnumerable<GetUserDto>> GetAllUsersAsync();


    /// Belirli bir kullanıcıyı ID ile getirir.
    Task<GetUserDto?> GetUserByIdAsync(long userId);


    /// Yeni kullanıcı oluşturur (sp_CreateUser ile).
    Task<bool> CreateUserAsync(CreateUserDto dto);


    /// Var olan kullanıcıyı günceller (sp_UpdateUsers ile).
    Task<bool> UpdateUserAsync(UpdateUserDto dto);

    /// Kullanıcıyı pasifleştirir (sp_DeactivateUser ile).
    Task<bool> DeactivateUserAsync(long userId);


    /// Kullanıcının aylık hareket özetini döner (sp_GetUserActivitySummary).
    Task<UserActivitySummaryDto?> GetUserActivitySummaryAsync(long userId);
}