using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class TokenStrategyFactory : ITokenStrategyFactory
    {
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly ICacheService _cache;
        private readonly ILoggerFactory _loggerFactory;           
    

        public TokenStrategyFactory(
            IConfiguration configuration,
            GestorInventarioContext context,
            ICacheService cache,
            ILoggerFactory loggerFactory                       
            )
        {
            _configuration = configuration;
            _context = context;
            _cache=cache;
            _loggerFactory = loggerFactory;
           
        }

        public ITokenStrategy CreateStrategy()
        {
            string authMode = _configuration["AuthMode"] ?? "AsymmetricDynamic";

            return authMode switch
            {
                "Symmetric" => new SymmetricTokenStrategy(_configuration, _context),

                "AsymmetricFixed" => new AsymmetricFixedTokenStrategy(_context, _configuration),

               
                "AsymmetricDynamic" => new AsymmetricDynamicTokenStrategy(_configuration,_loggerFactory.CreateLogger<AsymmetricDynamicTokenStrategy>(),_context,_cache),

                _ => throw new NotSupportedException($"Modo de autenticación no soportado: {authMode}")
            };
        }
    }
}