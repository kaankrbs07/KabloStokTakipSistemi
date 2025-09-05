using AutoMapper;
using KabloStokTakipSistemi.DTOs;
using KabloStokTakipSistemi.Models;

namespace KabloStokTakipSistemi.Mappingprofiles;

public class AlertProfile : Profile
{
    public AlertProfile()
    {
        CreateMap<Alert, GetAlertDto>();
    }
}