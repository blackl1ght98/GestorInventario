using AutoMapper;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Automaper
{
    public class ProvedorInfrastructureProfile:Profile

    {
        public ProvedorInfrastructureProfile()
        {
            CreateMap<EntityProveedor, Proveedore>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.NombreProveedor, opt => opt.MapFrom(src => src.Nombre))
                .ForMember(dest => dest.Contacto, opt => opt.MapFrom(src => src.Contacto))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(src => src.Direccion))
                .ForMember(dest => dest.IdUsuario, opt => opt.MapFrom(dest => dest.IdUsuario));
            CreateMap<Proveedore, EntityProveedor>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.NombreProveedor))
               .ForMember(dest => dest.Contacto, opt => opt.MapFrom(src => src.Contacto))
               .ForMember(dest => dest.Direccion, opt => opt.MapFrom(src => src.Direccion))
               .ForMember(dest => dest.IdUsuario, opt => opt.MapFrom(dest => dest.IdUsuario));
        }
    }
}
