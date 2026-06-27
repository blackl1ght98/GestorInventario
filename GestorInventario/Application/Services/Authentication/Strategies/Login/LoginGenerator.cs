using GestorInventario.Application.DTOS.User;
using GestorInventario.Domain.Models;

using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Utilities;
using GestorInventario.ViewModels.Usuarios;

namespace GestorInventario.Application.Services.Authentication.Strategies.Login
{
    public class LoginGenerator : ILoginGenerator
    {
        private readonly ILoginStrategy _strategy;
        public LoginGenerator( ILoginStrategyFactory factory)
        {
         
            _strategy = factory.GetStrategy();
        }
        public async Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto credencialesUsuario)
        {
          
        
            return await _strategy.AuthenticateAsync(credencialesUsuario);
        }
    }
}
