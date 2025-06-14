using AutoMapper;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Models.ViewModels;

namespace GestorInventario.Application.Classes
{
    public class UserProfile:Profile
    {
        public UserProfile()
        {
            CreateMap<UsuarioEditViewModel, Usuario>()
                .ForMember(dest => dest.ConfirmacionEmail, opt => opt.Ignore())
                .ForMember(dest => dest.IdRol, opt => opt.Ignore());
            CreateMap<EditarUsuarioActual, Usuario>()
               .ForMember(dest => dest.Email, opt => opt.Ignore())
               .ForMember(dest => dest.ConfirmacionEmail, opt => opt.Ignore())
                 .ForMember(dest => dest.Ciudad, opt => opt.MapFrom(src => src.ciudad))
            .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(src => src.codigoPostal));
        }
    }
}
