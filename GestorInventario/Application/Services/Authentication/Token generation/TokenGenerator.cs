using GestorInventario.Application.DTOs.User;

using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;

using StackExchange.Redis;


namespace GestorInventario.Application.Services
{
    /// <summary>
    /// Servicio responsable de obtener el usuario desde la base de datos
    /// y delegar la generación del token a la estrategia configurada.
    /// </summary>

    public class TokenGenerator : ITokenGenerator
    {
        private readonly GestorInventarioContext _context;
        private readonly ITokenStrategy _tokenStrategy;

      
        public TokenGenerator(GestorInventarioContext context, ITokenStrategyFactory factory)
        {
            _context = context;
            _tokenStrategy = factory.CreateStrategy(); 
        }

        public async Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

            if (usuarioDB == null)
                throw new ArgumentException("El usuario no existe en la base de datos.");

            return await _tokenStrategy.GenerateTokenAsync(usuarioDB);
        }
    }
}

