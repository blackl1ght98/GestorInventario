using AutoMapper;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.User;


namespace GestorInventario.Application.Automaper
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<EditUserDto, Usuario>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.FechaRegistro, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmacionEmail, opt => opt.Ignore())
                .ForMember(dest => dest.BajaUsuario, opt => opt.Ignore())
                .ForMember(dest => dest.IdRol, opt => opt.Ignore())
                .ForMember(dest => dest.NombreCompleto, opt => opt.MapFrom(x => x.NombreCompleto))
                .ForMember(dest => dest.Telefono, opt => opt.MapFrom(x => x.Telefono))
                .ForMember(dest => dest.FechaNacimiento, opt => opt.MapFrom(x => x.FechaNacimiento))
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(x => x.CodigoPostal))
                .ForMember(dest => dest.Ciudad, opt => opt.MapFrom(x => x.Ciudad))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(x => x.Direccion ?? "No especificada"));

            // RegisterUserDto → Usuario, si lo usas. Si no, lo borramos.
            CreateMap<RegisterUserDto, Usuario>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.FechaRegistro, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmacionEmail, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.BajaUsuario, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(x => x.CodigoPostal))
                .ForMember(dest => dest.Ciudad, opt => opt.MapFrom(x => x.Ciudad))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(x => x.Direccion ?? "No especificada"));
        }
    }
}
