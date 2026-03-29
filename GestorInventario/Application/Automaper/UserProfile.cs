using AutoMapper;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Models;
using GestorInventario.ViewModels.user;

namespace GestorInventario.Application.Classes
{
    public class UserProfile:Profile
    {
        public UserProfile()
        {
            CreateMap<UsuarioEditViewModel, Usuario>()
                .ForMember(dest => dest.ConfirmacionEmail, opt => opt.Ignore())
                .ForMember(dest => dest.IdRol, opt => opt.Ignore())
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(x => x.CodigoPostal))
                .ForMember(dest => dest.Email, opt => opt.Ignore());

            

            // Mapeo desde ViewModel → Entidad de Dominio
            CreateMap<UserViewModel, EntityUser>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.FechaRegistro, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmacionEmail, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.BajaUsuario, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(x => x.CodigoPostal))
                .ForMember(dest => dest.Ciudad, opt => opt.MapFrom(x => x.Ciudad))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(x => x.Direccion ?? "No especificada"));   // importante

        
            CreateMap<Usuario, UsuarioEditViewModel>()
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(src => src.CodigoPostal))
                .ForMember(dest => dest.EsEdicionPropia, opt => opt.Ignore());

        }
    }
}
