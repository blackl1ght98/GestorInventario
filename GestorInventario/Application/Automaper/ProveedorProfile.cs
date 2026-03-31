using AutoMapper;
using GestorInventario.Domain.Entities;
using GestorInventario.ViewModels.provider;

namespace GestorInventario.Application.Automaper
{
    public class ProveedorProfile:Profile
    {
        public ProveedorProfile()
        {
            CreateMap<ProveedorViewModel, EntityProveedor>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.NombreProveedor))
                .ForMember(dest => dest.Contacto, opt => opt.MapFrom(src => src.Contacto))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(src => src.Direccion))
                .ForMember(dest=>dest.IdUsuario,opt=>opt.MapFrom(dest=>dest.IdUsuario));
            CreateMap<EntityProveedor, ProveedorViewModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.NombreProveedor, opt => opt.MapFrom(src => src.Nombre))
                .ForMember(dest => dest.Contacto, opt => opt.MapFrom(src => src.Contacto))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(src => src.Direccion))
                .ForMember(dest => dest.IdUsuario, opt => opt.MapFrom(dest => dest.IdUsuario));
        }
    }
    }

