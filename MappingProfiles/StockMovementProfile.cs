using AutoMapper;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.DTOs;

namespace KabloStokTakipSistemi.MappingProfiles;

public class StockMovementProfile : Profile
{
    public StockMovementProfile()
    {
        // Model <-> Get DTO 
        CreateMap<StockMovement, GetStockMovementDto>();

        // Create DTO -> Model
        CreateMap<CreateStockMovementDto, StockMovement>();

        // Update DTO -> Model 
        CreateMap<UpdateStockMovementDto, StockMovement>()
            .ForAllMembers(opt => opt.Condition((src, dest, val) => val is not null));
    }
}
