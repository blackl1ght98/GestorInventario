using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication;
using GestorInventario.Shared.DTOS.Auth;
using Microsoft.Extensions.Configuration;


namespace GestorInventario.Application.Services.Authentication.Strategies.AccessToken
{
    public abstract class BaseTokenStrategy : ITokenStrategy
    {
        protected readonly IConfiguration _configuration;
        protected readonly ITokenClaimsBuilder _claimsBuilder;

        protected BaseTokenStrategy(IConfiguration configuration, ITokenClaimsBuilder claimsBuilder)
        {
            _configuration = configuration;
            _claimsBuilder = claimsBuilder;
        }

        public abstract Task<LoginResponseDto> GenerateTokenAsync(Usuario usuarioCompleto);

       
    }
}
