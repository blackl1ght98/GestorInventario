using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using GestorInventario.Interfaces.Application.Services.Authentication.Strategies.AccessToken;
using GestorInventario.Shared.DTOS.Auth;
using Microsoft.Extensions.Configuration;


namespace GestorInventario.Application.Services.Authentication.Strategies.AccessToken
{
    public abstract class BaseTokenStrategy : ITokenStrategy
    {
        protected readonly IConfiguration _configuration;
        protected readonly IJwtTokenSettings _claimsBuilder;

        protected BaseTokenStrategy(IConfiguration configuration, IJwtTokenSettings claimsBuilder)
        {
            _configuration = configuration;
            _claimsBuilder = claimsBuilder;
        }

        public abstract Task<LoginResponseDto> GenerateTokenAsync(Usuario usuarioCompleto);
    


    }
}
