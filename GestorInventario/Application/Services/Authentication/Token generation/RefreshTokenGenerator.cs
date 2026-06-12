using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Services.Authentication.Token_generation
{
    public class RefreshTokenGenerator: IRefreshTokenGenerator
    {
        private readonly GestorInventarioContext _context;
        private readonly IRefreshTokenStrategy _refreshTokenMethod;
        public RefreshTokenGenerator(GestorInventarioContext context, IRefreshTokenStrategyFactory factory)
        {
            _context = context;
            _refreshTokenMethod = factory.CreateStrategy();   
        }
        public async Task<string> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            // 1. Busco el usuario completo en la base de datos (con su Rol)
            var usuarioDB = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

            if (usuarioDB == null)
                throw new ArgumentException("El usuario no existe en la base de datos.");

            // 2. Le entrego el usuario a la estrategia que corresponde
            //    (Symmetric, AsymmetricFixed o AsymmetricDynamic)
            return await _refreshTokenMethod.GenerarTokenRefresco(usuarioDB);
        }
    }
}
