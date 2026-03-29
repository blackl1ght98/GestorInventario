using AutoMapper;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Automaper
{
    public class UserInfrastructureProfile:Profile
    {
        public UserInfrastructureProfile()
        {
            // Mapeo desde Entidad de Dominio → Entidad EF (para guardar)
            CreateMap<EntityUser, Usuario>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password))
                .ForMember(dest => dest.Salt, opt => opt.MapFrom(src => src.Salt))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.NombreCompleto, opt => opt.MapFrom(src => src.NombreCompleto))
                .ForMember(dest => dest.ConfirmacionEmail, opt => opt.MapFrom(src => src.ConfirmacionEmail))
                .ForMember(dest => dest.BajaUsuario, opt => opt.MapFrom(src => src.BajaUsuario))
                .ForMember(dest => dest.IdRol, opt => opt.MapFrom(src => src.IdRol))
                .ForMember(dest => dest.FechaRegistro, opt => opt.MapFrom(src => src.FechaRegistro ?? DateTime.Now))
                .ForMember(dest => dest.TemporaryPassword, opt => opt.MapFrom(src => src.TemporaryPassword))
                .ForMember(dest => dest.EmailVerificationToken, opt => opt.MapFrom(src => src.EmailVerificationToken))
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(src => src.CodigoPostal))
                .ForMember(dest => dest.Ciudad, opt => opt.MapFrom(src => src.Ciudad))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(src => src.Direccion))
                .ForMember(dest => dest.Telefono, opt => opt.MapFrom(src => src.Telefono))
                .ForMember(dest => dest.FechaNacimiento, opt => opt.MapFrom(src => src.FechaNacimiento));

            // Mapeo desde Entidad EF → Entidad de Dominio (para cargar datos)
            CreateMap<Usuario, EntityUser>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.NombreCompleto, opt => opt.MapFrom(src => src.NombreCompleto))
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password))
                .ForMember(dest => dest.Salt, opt => opt.MapFrom(src => src.Salt))
                .ForMember(dest => dest.ConfirmacionEmail, opt => opt.MapFrom(src => src.ConfirmacionEmail))
                .ForMember(dest => dest.BajaUsuario, opt => opt.MapFrom(src => src.BajaUsuario))
                .ForMember(dest => dest.IdRol, opt => opt.MapFrom(src => src.IdRol))
                .ForMember(dest => dest.FechaRegistro, opt => opt.MapFrom(src => src.FechaRegistro))
                .ForMember(dest => dest.TemporaryPassword, opt => opt.MapFrom(src => src.TemporaryPassword))
                .ForMember(dest => dest.EmailVerificationToken, opt => opt.MapFrom(src => src.EmailVerificationToken))
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(src => src.CodigoPostal))
                .ForMember(dest => dest.Ciudad, opt => opt.MapFrom(src => src.Ciudad))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(src => src.Direccion))
                .ForMember(dest => dest.Telefono, opt => opt.MapFrom(src => src.Telefono))
                .ForMember(dest => dest.FechaNacimiento, opt => opt.MapFrom(src => src.FechaNacimiento));
        }

    }
}
