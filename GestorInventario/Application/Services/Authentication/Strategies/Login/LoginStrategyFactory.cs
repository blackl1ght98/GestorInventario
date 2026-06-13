using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;

namespace GestorInventario.Application.Services.Authentication.Strategies.Login
{
    public class LoginStrategyFactory : ILoginStrategyFactory
    {

        private readonly IAuthService _authService;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly ICacheService _cache;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        public LoginStrategyFactory(IAuthService authService, IPolicyExecutor policyExecutor, ICacheService cache, IEmailService emailService, IConfiguration configuration)
        {
            _authService = authService;
            _policyExecutor = policyExecutor;
            _cache = cache;
            _emailService = emailService;
            _configuration = configuration;
        }

        public ILoginStrategy GetStrategy()
        {
            var mfaEnabled = _configuration.GetValue<bool>("IsMfaEnabled");
            return mfaEnabled switch
            {
                true => new MfaLoginStrategy(_authService, _policyExecutor, _cache, _emailService),
                false=> new StandardLoginStrategy(_authService,_policyExecutor)
            };
                   
        }
    }
}
