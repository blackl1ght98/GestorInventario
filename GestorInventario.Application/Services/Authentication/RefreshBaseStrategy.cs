using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Services.Authentication
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
