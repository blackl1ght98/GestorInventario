using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Caching;
using GestorInventario.Application.Interfaces.EmailServices;
using GestorInventario.Application.Interfaces.Resilience;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Services.Authentication
{
    public class MfaLoginStrategy : ILoginStrategy
    {
        private readonly IAuthService _authService;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IHybridCacheService _cache;
        private readonly IEmailService _emailService;

        public MfaLoginStrategy(IAuthService authService, IPolicyExecutor policyExecutor, IHybridCacheService cache, IEmailService emailService)
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

            // Retornamos un OK, pero indicamos que REQUIERE MFA 
            return OperationResult<AuthSessionDetails>.Ok( new AuthSessionDetails(user.Data, true),"Código MFA enviado");
        }
    }
}
