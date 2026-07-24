using GestorInventario.Interfaces.Application.Email;
using GestorInventario.Interfaces.Application.RetryPolicy;
using GestorInventario.Interfaces.Application.Services.Authentication;
using GestorInventario.Interfaces.Application.Services.Common;
using GestorInventario.Shared.DTOS.Auth;
using GestorInventario.Shared.Utilities;



namespace GestorInventario.Application.Services.Authentication.Strategies.Login
{
    public class MfaLoginStrategy : ILoginStrategy
    {
        private readonly IAuthService _authService;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly ICacheService _cache;
        private readonly IEmailService _emailService;

        public MfaLoginStrategy(IAuthService authService, IPolicyExecutor policyExecutor, ICacheService cache, IEmailService emailService)
        {
            _authService = authService;
            _policyExecutor = policyExecutor;
            _cache = cache;
            _emailService = emailService;
        }

        public async Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto model)
        {
            var user = await _policyExecutor.ExecutePolicyAsync(() => _authService.Login(model.Email, model));

            if (!user.Success || user.Data == null)
            {
                return OperationResult<AuthSessionDetails>.Fail(user.Message);
            }

            string mfaCode = new Random().Next(100000, 999999).ToString();
            await _cache.SetStringAsync($"MFA_{user.Data.Id}", mfaCode, TimeSpan.FromMinutes(5));
            await _emailService.SendMfaCodeEmail(user.Data.Email, mfaCode);

            // Retornamos un OK, pero indicamos que REQUIERE MFA y pasamos el código
            return OperationResult<AuthSessionDetails>.Ok("Código MFA enviado", new AuthSessionDetails(user.Data, true, mfaCode));
        }
    }
}
