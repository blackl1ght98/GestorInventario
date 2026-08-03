using AutoMapper;
using GestorInventario.Domain.Models;
using GestorInventario.ViewModels.Users;

namespace GestorInventario.AutoMapper
{
    public class UserViewModelProfile : Profile
    {
        public UserViewModelProfile()
        {
            CreateMap<Usuario, EditUserFormViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(x => x.Id))
                .ForMember(dest => dest.NombreCompleto, opt => opt.MapFrom(x => x.NombreCompleto))
                .ForMember(dest => dest.Telefono, opt => opt.MapFrom(x => x.Telefono))
                .ForMember(dest => dest.FechaNacimiento, opt => opt.MapFrom(x => x.FechaNacimiento))
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(x => x.CodigoPostal))
                .ForMember(dest => dest.Ciudad, opt => opt.MapFrom(x => x.Ciudad))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(x => x.Direccion ?? "No especificada"));
        }
    }
}
