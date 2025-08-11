using AutoMapper;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Models;

namespace KabloStokTakipSistemi.MappingProfiles;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // 🔹 User
        CreateMap<User, GetUserDto>()
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department.DepartmentName));
        CreateMap<CreateUserDto, User>();

        // 🔹 Admin
        CreateMap<Admin, GetAdminDto>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.User.Department.DepartmentName));
        CreateMap<CreateAdminDto, Admin>();
        CreateMap<UpdateAdminDto, Admin>();

        // 🔹 Employee
        CreateMap<Employee, GetEmployeeDto>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.User.Department.DepartmentName));
        CreateMap<CreateEmployeeDto, Employee>();
        CreateMap<UpdateEmployeeDto, Employee>();

        // 🔹 User Activity Summary (SP verisi)
        CreateMap<User, UserActivitySummaryDto>(); // Eğer ihtiyaç olursa kullanılabilir
    }
}