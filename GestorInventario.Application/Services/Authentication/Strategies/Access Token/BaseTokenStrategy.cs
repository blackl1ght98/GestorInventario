
using GestorInventario.Domain.Models;

using GestorInventario.Interfaces.Application.Authentication;

using GestorInventario.Shared.DTOS.User;
using Microsoft.Extensions.Configuration;


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

       
    }
}
