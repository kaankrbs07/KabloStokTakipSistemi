using AutoMapper;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.DTOs;

namespace KabloStokTakipSistemi.MappingProfiles
{
    public class LogProfile : Profile
    {
        public LogProfile()
        {
            // Log -> LogDto : doğrudan map
            CreateMap<Log, LogDto>();


            CreateMap<Log, UserLogDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                    src.User == null
                        ? string.Empty
                        : $"{src.User.FirstName ?? string.Empty} {src.User.LastName ?? string.Empty}".Trim()));
        }
    }
}