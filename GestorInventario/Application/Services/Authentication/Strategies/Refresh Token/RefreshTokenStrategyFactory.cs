using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using System.Configuration;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class RefreshTokenStrategyFactory : IRefreshTokenStrategyFactory
    {

        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly ITokenStrategyFactory _tokenStrategyFactory;
       private readonly ILoggerFactory _loggerFactory;
        private readonly ICacheService _cacheService;
        public RefreshTokenStrategyFactory(IConfiguration configuration, GestorInventarioContext context, ITokenStrategyFactory tokenStrategyFactory, ILoggerFactory loggerFactory, ICacheService cache)
        {
            _configuration = configuration;
            _context = context;
            _tokenStrategyFactory = tokenStrategyFactory;
            _loggerFactory = loggerFactory;
            _cacheService = cache;
        }

        public IRefreshTokenStrategy CreateStrategy()
        {
            string authMode = _configuration["AuthMode"] ?? "AsymmetricDynamic";

            return authMode switch
            {
                "Symmetric" => new RefreshSymmetricToken(_context,_tokenStrategyFactory,_configuration, _loggerFactory.CreateLogger<RefreshSymmetricToken>()),

                "AsymmetricFixed" => new RefreshAsymetricFixedToken(_context,_tokenStrategyFactory,_configuration),


                "AsymmetricDynamic" => new RefreshAsymmetricDynamicToken(_context,_tokenStrategyFactory,_cacheService),

                _ => throw new NotSupportedException($"Modo de autenticación no soportado: {authMode}")
            };
        }
    }
}
