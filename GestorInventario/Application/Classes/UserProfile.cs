using AutoMapper;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Domain.Models.ViewModels.user;

namespace GestorInventario.Application.Classes
{
    public class UserProfile:Profile
    {
        public UserProfile()
        {
            CreateMap<UsuarioEditViewModel, Usuario>()
                .ForMember(dest => dest.ConfirmacionEmail, opt => opt.Ignore())
                .ForMember(dest => dest.IdRol, opt => opt.Ignore())
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(x => x.codigoPostal))
                .ForMember(dest => dest.Email, opt => opt.Ignore());

            CreateMap<UserViewModel, Usuario>()
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.FechaRegistro, opt => opt.Ignore())
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(x => x.codigoPostal))
                .ForMember(dest => dest.Ciudad, opt => opt.MapFrom(x => x.ciudad));
            CreateMap<Usuario, UsuarioEditViewModel>()
                .ForMember(dest => dest.codigoPostal, opt => opt.MapFrom(src => src.CodigoPostal))
                .ForMember(dest => dest.EsEdicionPropia, opt => opt.Ignore());

        }
    }
}
