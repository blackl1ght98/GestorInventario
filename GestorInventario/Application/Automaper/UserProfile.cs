using AutoMapper;
using GestorInventario.Domain.Entities;
using GestorInventario.ViewModels.user;

namespace GestorInventario.Application.Classes
{
    public class UserProfile:Profile
    {
        public UserProfile()
        {
           
            // Mapeo desde ViewModel de edición → Entidad de Dominio
            CreateMap<UsuarioEditViewModel, EntityUser>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.FechaRegistro, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmacionEmail, opt => opt.Ignore())   
                .ForMember(dest => dest.BajaUsuario, opt => opt.Ignore())
                .ForMember(dest => dest.NombreCompleto, opt => opt.MapFrom(x => x.NombreCompleto))
                .ForMember(dest => dest.Telefono, opt => opt.MapFrom(x => x.Telefono))
                .ForMember(dest => dest.FechaNacimiento, opt => opt.MapFrom(x => x.FechaNacimiento))
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(x => x.CodigoPostal))
                .ForMember(dest => dest.Ciudad, opt => opt.MapFrom(x => x.Ciudad))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(x => x.Direccion ?? "No especificada"))
                .ForMember(dest => dest.IdRol, opt => opt.Ignore());  



            // Mapeo desde ViewModel → Entidad de Dominio: Para la creacion
            CreateMap<UserViewModel, EntityUser>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.FechaRegistro, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmacionEmail, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.BajaUsuario, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(x => x.CodigoPostal))
                .ForMember(dest => dest.Ciudad, opt => opt.MapFrom(x => x.Ciudad))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(x => x.Direccion ?? "No especificada"));

            // Mapeo usado para mostrar los datos en el formulario de edición
           
            CreateMap<EntityUser, UsuarioEditViewModel>()
                .ForMember(dest => dest.NombreCompleto, opt => opt.MapFrom(src => src.NombreCompleto))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Telefono, opt => opt.MapFrom(src => src.Telefono))
                .ForMember(dest => dest.FechaNacimiento, opt => opt.MapFrom(src => src.FechaNacimiento))
                .ForMember(dest => dest.CodigoPostal, opt => opt.MapFrom(src => src.CodigoPostal))
                .ForMember(dest => dest.Ciudad, opt => opt.MapFrom(src => src.Ciudad))
                .ForMember(dest => dest.Direccion, opt => opt.MapFrom(src => src.Direccion))             
                .ForMember(dest => dest.EsEdicionPropia, opt => opt.Ignore());   

        }
    }
}
