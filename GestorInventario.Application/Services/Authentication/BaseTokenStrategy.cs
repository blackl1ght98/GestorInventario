using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace GestorInventario.Application.Services.Authentication
{
    public abstract class BaseTokenStrategy : ITokenStrategy
    {
        protected readonly IConfiguration _configuration;
        protected readonly IJwtTokenSettings _jwtTokenSettings;

        protected BaseTokenStrategy(IConfiguration configuration, IJwtTokenSettings jwtTokenSettings)
        {
            _configuration = configuration;
            _jwtTokenSettings = jwtTokenSettings;
        }

        public abstract Task<LoginResponseDto> GenerateTokenAsync(Usuario usuarioCompleto);
    


    }
}
