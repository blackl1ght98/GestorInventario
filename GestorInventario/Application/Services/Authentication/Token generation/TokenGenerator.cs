using GestorInventario.Application.DTOs.User;
using GestorInventario.Application.Services.Authentication.Strategies;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;


namespace GestorInventario.Application.Services
{
    public class TokenGenerator : ITokenGenerator
    {

        private readonly GestorInventarioContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<TokenGenerator> _logger;
        private readonly IDistributedCache _redis;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ITokenStrategy _tokenStrategy;

        public TokenGenerator(GestorInventarioContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor,
            IMemoryCache memoryCache, ILogger<TokenGenerator> logger, IDistributedCache cache, IConnectionMultiplexer connection)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _memoryCache = memoryCache;
            _logger = logger;
            _redis = cache;
            _connectionMultiplexer = connection;

            // Seleccionar estrategia según configuración
            string authMode = configuration["AuthMode"] ?? "Symmetric";
            _tokenStrategy = authMode switch
            {
                "Symmetric" => new SymmetricTokenStrategy(configuration, context),
                "AsymmetricFixed" => new AsymmetricFixedTokenStrategy(context, configuration),
                "AsymmetricDynamic" => new AsymmetricDynamicTokenStrategy(configuration, cache, memoryCache, connection, logger, context),
                _ => throw new NotSupportedException("Modo de autenticación no soportado")
            };
        }

        public async Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

            if (usuarioDB == null)
            {
                throw new ArgumentException("El usuario no existe en la base de datos.");
            }

            // Usar la estrategia seleccionada para generar el token
            return await _tokenStrategy.GenerateTokenAsync(usuarioDB);
        }

     
       
    }
}

