using AutoMapper;
using KabloStokTakipSistemi.DTOs.Users;
using KabloStokTakipSistemi.Models;

namespace KabloStokTakipSistemi.MappingProfiles;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // User -> GetUserDto
        CreateMap<User, GetUserDto>()
            .ForMember(d => d.DepartmentName,
                opt => opt.MapFrom(s => s.Department != null ? s.Department.DepartmentName : null));

        // CreateUserDto -> User
        CreateMap<CreateUserDto, User>();

        // UpdateUserDto -> User 
        CreateMap<UpdateUserDto, User>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember is not null));

        // Admin -> GetAdminDto 
        CreateMap<Admin, GetAdminDto>()
            .ForMember(d => d.FirstName,
                opt => opt.MapFrom(s => s.User != null ? s.User.FirstName : null))
            .ForMember(d => d.LastName,
                opt => opt.MapFrom(s => s.User != null ? s.User.LastName : null))
            .ForMember(d => d.DepartmentName,
                opt => opt.MapFrom(s => s.User != null && s.User.Department != null
                    ? s.User.Department.DepartmentName
                    : null));
        CreateMap<CreateAdminDto, Admin>();
        CreateMap<UpdateAdminDto, Admin>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember is not null));

        // Employee -> GetEmployeeDto
        CreateMap<Employee, GetEmployeeDto>()
            .ForMember(d => d.FirstName,
                opt => opt.MapFrom(s => s.User != null ? s.User.FirstName : null))
            .ForMember(d => d.LastName,
                opt => opt.MapFrom(s => s.User != null ? s.User.LastName : null))
            .ForMember(d => d.DepartmentName,
                opt => opt.MapFrom(s => s.User != null && s.User.Department != null
                    ? s.User.Department.DepartmentName
                    : null));
        CreateMap<CreateEmployeeDto, Employee>();
        CreateMap<UpdateEmployeeDto, Employee>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember is not null));

    }
}
