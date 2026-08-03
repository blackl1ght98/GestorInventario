using GestorInventario.Domain.enums.Usuario;
using System;
using System.Collections.Generic;
using System.Text;

namespace GestorInventario.Application.Services.User
{
    public static class Roles
    {
        public const string Administrador = nameof(Rol.Administrador);
        public const string Usuario = nameof(Rol.Usuario);
        public const string DefaultRegistro = Usuario; 
    }
}
