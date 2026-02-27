using GestorInventario.Application.Services.Authentication.Strategies;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace GestorInventario.Application.Services.Authentication.Token_generation
{
    /// <summary>
    /// Fábrica que crea la estrategia de generación de tokens adecuada según el modo configurado en "AuthMode".
    /// Soporta Symmetric, AsymmetricFixed y AsymmetricDynamic.
    /// </summary>
    public class TokenStrategyFactory : ITokenStrategyFactory
    {
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly IDistributedCache _redis;
        private readonly IMemoryCache _memoryCache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<TokenGenerator> _logger;
        private readonly IEncryptionService _encryptionService;
        public TokenStrategyFactory(
            IConfiguration configuration,
            GestorInventarioContext context,
            IDistributedCache redis,
            IMemoryCache memoryCache,
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<TokenGenerator> logger,
            IEncryptionService encryptation)
        {
            _configuration = configuration;
            _context = context;
            _redis = redis;
            _memoryCache = memoryCache;
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
            _encryptionService = encryptation;
        }
        
        public ITokenStrategy CreateStrategy()
        {
            string authMode = _configuration["AuthMode"] ?? "Symmetric";

            return authMode switch
            {
                "Symmetric" => new SymmetricTokenStrategy(_configuration, _context),
                "AsymmetricFixed" => new AsymmetricFixedTokenStrategy(_context, _configuration),
                "AsymmetricDynamic" => new AsymmetricDynamicTokenStrategy(_configuration, _redis, _memoryCache, _connectionMultiplexer, _logger, _context,_encryptionService),
                _ => throw new NotSupportedException($"Modo de autenticación no soportado: {authMode}")
            };
        }
    }
}
