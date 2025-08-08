using AutoMapper;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Models;

namespace KabloStokTakipSistemi.MappingProfiles;

public class DepartmentProfile : Profile
{
    public DepartmentProfile()
    {
        CreateMap<Department, GetDepartmentDto>()
            .ForMember(dest => dest.AdminUsername, opt => opt.MapFrom(src => src.Admin.Username));

        CreateMap<CreateDepartmentDto, Department>();
        CreateMap<UpdateDepartmentDto, Department>();
    }
}