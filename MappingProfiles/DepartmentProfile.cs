using AutoMapper;
using KabloStokTakipSistemi.DTOs; // GetDepartmentDto, CreateDepartmentDto, UpdateDepartmentDto
using KabloStokTakipSistemi.Models; // Department, Admin

namespace KabloStokTakipSistemi.MappingProfiles;

public class DepartmentProfile : Profile
{
    public DepartmentProfile()
    {
        // Entity -> DTO (liste/detay)
        CreateMap<Department, GetDepartmentDto>()
            .ForMember(d => d.DepartmentID, o => o.MapFrom(s => s.DepartmentID))
            .ForMember(d => d.DepartmentName, o => o.MapFrom(s => s.DepartmentName))
            .ForMember(d => d.AdminID, o => o.MapFrom(s => s.AdminID))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt));

        // Create DTO -> Entity
        CreateMap<CreateDepartmentDto, Department>()
            .ForMember(e => e.DepartmentID, o => o.Ignore()) // PK DB oluşturur
            .ForMember(e => e.CreatedAt, o => o.Ignore()); // DB default/trigger’a bırak

        // Update DTO -> Entity (CreatedAt asla değişmesin)
        CreateMap<UpdateDepartmentDto, Department>()
            .ForMember(e => e.DepartmentID, o => o.Ignore())
            .ForMember(e => e.CreatedAt, o => o.Ignore());
    }
}