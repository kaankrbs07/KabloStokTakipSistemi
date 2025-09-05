using AutoMapper;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.DTOs.Users;

namespace KabloStokTakipSistemi.MappingProfiles;

public class ReportProfile : Profile
{
    public ReportProfile()
    {
        CreateMap<MonthlyMultiCableReportDto, MonthlyMultiCableReportDto>();
        CreateMap<MonthlySingleCableReportDto, MonthlySingleCableReportDto>();
        CreateMap<UserActivitySummaryDto, UserActivitySummaryDto>();
    }
}