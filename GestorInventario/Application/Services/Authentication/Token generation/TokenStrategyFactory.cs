using GestorInventario.Application.Services.Authentication.Strategies;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace GestorInventario.Application.Services.Authentication.Token_generation
{
    /**
     Clase encargada de crear la estrategia de autenticacion
     */
    public class TokenStrategyFactory : ITokenStrategyFactory
    {
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly IDistributedCache _redis;
        private readonly IMemoryCache _memoryCache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<TokenGenerator> _logger;

        public TokenStrategyFactory(
            IConfiguration configuration,
            GestorInventarioContext context,
            IDistributedCache redis,
            IMemoryCache memoryCache,
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<TokenGenerator> logger)
        {
            _configuration = configuration;
            _context = context;
            _redis = redis;
            _memoryCache = memoryCache;
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
        }
        
        public ITokenStrategy CreateStrategy()
        {
            string authMode = _configuration["AuthMode"] ?? "Symmetric";

            return authMode switch
            {
                "Symmetric" => new SymmetricTokenStrategy(_configuration, _context),
                "AsymmetricFixed" => new AsymmetricFixedTokenStrategy(_context, _configuration),
                "AsymmetricDynamic" => new AsymmetricDynamicTokenStrategy(_configuration, _redis, _memoryCache, _connectionMultiplexer, _logger, _context),
                _ => throw new NotSupportedException($"Modo de autenticación no soportado: {authMode}")
            };
        }
    }
}
