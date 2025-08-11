
using AutoMapper;
using KabloStokTakipSistemi.DTOs;             
using KabloStokTakipSistemi.DTOs.Users; 

namespace KabloStokTakipSistemi.MappingProfiles;

public class ReportProfile : Profile
{
    public ReportProfile()
    {
        // --- Aylık rapor SP DTO’ları (passthrough) ---
        CreateMap<MonthlyMultiCableReportDto, MonthlyMultiCableReportDto>();
        CreateMap<MonthlySingleCableReportDto, MonthlySingleCableReportDto>();

        // --- Kullanıcı aktivite özeti SP DTO’su (passthrough) ---
        CreateMap<UserActivitySummaryDto, UserActivitySummaryDto>();
    }
}