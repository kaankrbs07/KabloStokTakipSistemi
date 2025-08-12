using AutoMapper;
using KabloStokTakipSistemi.DTOs.Cables;
using KabloStokTakipSistemi.Models;

namespace KabloStokTakipSistemi.MappingProfiles;

/// <summary>
/// AutoMapper profile for cable-related entities and DTOs
/// </summary>
public sealed class CableProfile : Profile
{
    public CableProfile()
    {
        ConfigureSingleCableMappings();
        ConfigureMultiCableMappings();
        ConfigureMultiCableContentMappings();
        ConfigureThresholdMappings();
    }

    private void ConfigureSingleCableMappings()
    {
        // SingleCable -> GetSingleCableDto
        CreateMap<SingleCable, GetSingleCableDto>();

        // CreateSingleCableDto -> SingleCable
        CreateMap<CreateSingleCableDto, SingleCable>();

        // UpdateSingleCableDto -> SingleCable (null-safe updates)
        CreateMap<UpdateSingleCableDto, SingleCable>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember is not null));
    }

    private void ConfigureMultiCableMappings()
    {
        // MultiCable -> GetMultiCableDto
        CreateMap<MultiCable, GetMultiCableDto>();

        // CreateMultiCableDto -> MultiCable
        CreateMap<CreateMultiCableDto, MultiCable>();

        // UpdateMultiCableDto -> MultiCable (null-safe updates)
        CreateMap<UpdateMultiCableDto, MultiCable>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember is not null));
    }

    private void ConfigureMultiCableContentMappings()
    {
        // MultiCableContent -> GetMultiCableContentDto
        CreateMap<MultiCableContent, GetMultiCableContentDto>()
            .ForMember(dest => dest.SingleCableColor,
                opt => opt.MapFrom(src => src.SingleCable.Color));

        // CreateMultiCableContentDto -> MultiCableContent
        CreateMap<CreateMultiCableContentDto, MultiCableContent>();
    }

    private void ConfigureThresholdMappings()
    {
        // CableThreshold -> GetCableThresholdDto
        CreateMap<CableThreshold, GetCableThresholdDto>()
            .ForMember(dest => dest.CableName,
                opt => opt.MapFrom(src => src.MultiCable != null ? src.MultiCable.CableName : string.Empty));

        // CreateCableThresholdDto -> CableThreshold
        CreateMap<CreateCableThresholdDto, CableThreshold>();

        // UpdateCableThresholdDto -> CableThreshold (null-safe updates)
        CreateMap<UpdateCableThresholdDto, CableThreshold>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember is not null));

        // ColorThreshold mappings
        CreateMap<ColorThreshold, GetColorThresholdDto>();
        CreateMap<CreateColorThresholdDto, ColorThreshold>();
        CreateMap<UpdateColorThresholdDto, ColorThreshold>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember is not null));
    }
}