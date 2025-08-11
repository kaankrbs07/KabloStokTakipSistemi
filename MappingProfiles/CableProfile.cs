using AutoMapper;
using KabloStokTakipSistemi.Models;
using KabloStokTakipSistemi.DTOs.Cables;

namespace KabloStokTakipSistemi.MappingProfiles;

public class CableProfile : Profile
{
    public CableProfile()
    {
        // ----------------------
        // SingleCable <-> DTOs
        // ----------------------
        CreateMap<SingleCable, GetSingleCableDto>();

        CreateMap<CreateSingleCableDto, SingleCable>();

        // Update: null alanlar mevcut entity değerini EZMESİN
        CreateMap<UpdateSingleCableDto, SingleCable>()
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember is not null));

        // ----------------------
        // MultipleCable <-> DTOs
        // ----------------------
        CreateMap<MultiCable, GetMultiCableDto>(); // isim düzeltildi

        CreateMap<CreateMultiCableDto, MultiCable>();

        // Update: null-ignore
        CreateMap<UpdateMultiCableDto, MultiCable>()
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember is not null));

        // ----------------------
        // MultiCableContent <-> DTOs
        // ----------------------
        CreateMap<MultiCableContent, GetMultiCableContentDto>()
            .ForMember(d => d.SingleCableColor,
                m => m.MapFrom(s => s.SingleCable.Color));

        CreateMap<CreateMultiCableContentDto, MultiCableContent>();
        // Not: UpdateMultiCableContentDto tanımlı olmadığı için map kaldırıldı.

        // ----------------------
        // CableThreshold <-> DTOs
        // ----------------------
        CreateMap<CableThreshold, GetCableThresholdDto>()
            .ForMember(d => d.CableName,
                m => m.MapFrom(s => s.MultiCable.CableName));
        CreateMap<CreateCableThresholdDto, CableThreshold>();
        CreateMap<UpdateCableThresholdDto, CableThreshold>()
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember is not null));

        // ----------------------
        // ColorThreshold <-> DTOs
        // ----------------------
        CreateMap<ColorThreshold, GetColorThresholdDto>();
        CreateMap<CreateColorThresholdDto, ColorThreshold>();
        CreateMap<UpdateColorThresholdDto, ColorThreshold>()
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) => srcMember is not null));
    }
}