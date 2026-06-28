
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.User;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public abstract class BaseTokenStrategy : ITokenStrategy
    {
        protected readonly IConfiguration _configuration;
        protected readonly TokenClaimsBuilder _claimsBuilder;

        protected BaseTokenStrategy(IConfiguration configuration, TokenClaimsBuilder claimsBuilder)
        {
            _configuration = configuration;
            _claimsBuilder = claimsBuilder;
        }

        public abstract Task<LoginResponseDto> GenerateTokenAsync(Usuario usuarioCompleto);

        // Mantener por compatibilidad (lo usan los Refresh* actuales)
        protected List<Claim> CrearClaims(Usuario u) => _claimsBuilder.CrearClaims(u);
        protected string ObtenerIssuer() => _claimsBuilder.ObtenerIssuer();
        protected string ObtenerAudience() => _claimsBuilder.ObtenerAudience();
    }
}
