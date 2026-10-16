using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.Jwt;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.Strategies.AccessToken;
using GestorInventario.Domain.Models;
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
