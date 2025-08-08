using AutoMapper;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.DTOs;

namespace KabloStokTakipSistemi.MappingProfiles;

public class AlertProfile : Profile
{
    public AlertProfile()
    {
        // Sadece GetAlertDto var çünkü trigger ile oluşuyor
        CreateMap<Alert, GetAlertDto>();
    }
}