using AutoMapper;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.DTOs;

namespace KabloStokTakipSistemi.MappingProfiles;

public class StockMovementProfile : Profile
{
    public StockMovementProfile()
    {
        CreateMap<StockMovement, GetStockMovementDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
                src.User.Role == "Admin"
                    ? src.User.Admin.Username
                    : src.User.Employee.EmployeeID.ToString()));

        CreateMap<CreateStockMovementDto, StockMovement>();
        CreateMap<UpdateStockMovementDto, StockMovement>();
    }
}