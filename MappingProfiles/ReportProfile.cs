using AutoMapper;
using KabloStokTakipSistemi.DTOs;

namespace KabloStokTakipSistemi.MappingProfiles;

public class ReportProfile : Profile
{
    public ReportProfile()
    {
        // SP direkt DTO'ya döneceği için genelde mapper gerekmez ama AutoMapper kullanılıyorsa:
        CreateMap<CableReportDto, CableReportDto>();
        CreateMap<MultiCableReportDto, MultiCableReportDto>();
    }
}