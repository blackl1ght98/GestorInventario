using AutoMapper;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Automaper
{
    public class RolInfrastructureProfile:Profile
    {
        public RolInfrastructureProfile() { 
            CreateMap<EntityRoles, Role>()
                .ForMember(dest=>dest.Id, opt=>opt.Ignore())
                .ForMember(dest=>dest.Nombre, opt=>opt.MapFrom(dest=>dest.NombreRol));

            CreateMap<Role, EntityRoles>()
                  .ForMember(dest => dest.Id, opt => opt.Ignore())
                  .ForMember(dest => dest.NombreRol, opt => opt.MapFrom(dest => dest.Nombre));
        }
    }
}
