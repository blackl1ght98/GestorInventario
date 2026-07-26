using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using GestorInventario.Interfaces.Application.Services.Authentication.Strategies.RefreshToken;
using Microsoft.Extensions.Configuration;


namespace GestorInventario.Application.Services.Authentication.Strategies.RefreshToken
{
    public abstract class RefreshBaseStrategy: IRefreshTokenStrategy
    {
        protected readonly IJwtTokenSettings _claimsBuilder;
       
        protected RefreshBaseStrategy(IJwtTokenSettings claimsBuilder)
        {
           
            _claimsBuilder = claimsBuilder;
            
        }
        public abstract Task<string> GenerarTokenRefresco(Usuario credencialesUsuario);
    }
}
