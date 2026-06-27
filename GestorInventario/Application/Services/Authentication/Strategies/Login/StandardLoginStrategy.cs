using GestorInventario.Application.DTOS.User;

using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Utilities;
using GestorInventario.ViewModels.Usuarios;

namespace GestorInventario.Application.Services.Authentication.Strategies.Login
{
    public class StandardLoginStrategy : ILoginStrategy
    {
        private readonly IAuthService _authService;
        private readonly IPolicyExecutor _policyExecutor;

        public StandardLoginStrategy(IAuthService authService, IPolicyExecutor policyExecutor)
        {
            _authService = authService;
            _policyExecutor = policyExecutor;
        }

        public async Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginViewModel model)
        {
            var user = await _policyExecutor.ExecutePolicyAsync(() => _authService.Login(model.Email, model));

            if (!user.Success || user.Data == null)
            {
                return OperationResult<AuthSessionDetails>.Fail(user.Message);
            }

            // Retornamos un OK con los datos de sesión (MFA = false por defecto)
            return OperationResult<AuthSessionDetails>.Ok("Login exitoso", new AuthSessionDetails(user.Data));
        }
    }
}
