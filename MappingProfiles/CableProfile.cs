using AutoMapper;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.DTOs.Cables;

namespace KabloStokTakipSistemi.MappingProfiles;

public class CableProfile : Profile
{
    public CableProfile()
    {
        // SingleCable
        CreateMap<SingleCable, GetSingleCableDto>();
        CreateMap<CreateSingleCableDto, SingleCable>();
        CreateMap<UpdateSingleCableDto, SingleCable>();

        // MultipleCable
        CreateMap<MultipleCable, GetMultipleCableDto>();
        CreateMap<CreateMultipleCableDto, MultipleCable>();
        CreateMap<UpdateMultipleCableDto, MultipleCable>();

        //  MultiCableContent
        CreateMap<MultiCableContent, GetMultiCableContentDto>()
            .ForMember(dest => dest.SingleCableColor, opt => opt.MapFrom(src => src.SingleCable.Color));
        CreateMap<CreateMultiCableContentDto, MultiCableContent>();
        CreateMap<UpdateMultiCableContentDto, MultiCableContent>();

        // CableThreshold
        CreateMap<CableThreshold, GetCableThresholdDto>()
            .ForMember(dest => dest.CableName, opt => opt.MapFrom(src => src.MultiCable.CableName));
        CreateMap<CreateCableThresholdDto, CableThreshold>();
        CreateMap<UpdateCableThresholdDto, CableThreshold>();

        // ColorThreshold
        CreateMap<ColorThreshold, GetColorThresholdDto>();
        CreateMap<CreateColorThresholdDto, ColorThreshold>();
        CreateMap<UpdateColorThresholdDto, ColorThreshold>();
    }
}