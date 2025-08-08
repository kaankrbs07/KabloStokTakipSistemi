using AutoMapper;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.DTOs;

namespace KabloStokTakipSistemi.MappingProfiles;

public class LogProfile : Profile
{
    public LogProfile()
    {
        CreateMap<Log, GetLogDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src =>
                src.User.Role == "Admin"
                    ? src.User.Admin.Username
                    : src.User.Employee.EmployeeID.ToString()));

        CreateMap<Log, UserLogDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                src.User.FirstName + " " + src.User.LastName));
    }
}