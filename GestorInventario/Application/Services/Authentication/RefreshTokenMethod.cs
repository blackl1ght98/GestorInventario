﻿using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Application.Services.Authentication
{
    public class RefreshTokenMethod: IRefreshTokenMethod
    {
        private GestorInventarioContext _context;
        private IConfiguration _configuration;
        private IMemoryCache _memoryCache;
        private IDistributedCache _redis;
        private IConnectionMultiplexer _connectionMultiplexer;
        private ILogger<RefreshTokenMethod> _logger;

        public RefreshTokenMethod(GestorInventarioContext context, IConfiguration configuration, IMemoryCache memoryCache, IDistributedCache redis, IConnectionMultiplexer connectionMultiplexer, ILogger<RefreshTokenMethod> logger)
        {
            _context = context;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _redis = redis;
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
        }

        public async Task<string> GenerarTokenRefresco(Usuario credencialesUsuario)
        {
            // Obtener usuario de la base de datos
            //var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);
            var usuarioDB = await _context.Usuarios
      .Include(u => u.IdRolNavigation)
      .ThenInclude(r => r.RolePermisos)
      .ThenInclude(rp => rp.Permiso)
      .FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);
            if (usuarioDB == null)
            {
                throw new ArgumentException("El usuario no existe en la base de datos.");
            }

            // Definir Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())
            };
            var permisos = usuarioDB.IdRolNavigation.RolePermisos?.Select(rp => rp.Permiso?.Nombre) ?? Enumerable.Empty<string>();
            var permisosList = permisos.ToList();
            foreach (var permiso in permisosList)
            {
                if (!string.IsNullOrEmpty(permiso))
                {
                    claims.Add(new Claim("permiso", permiso, ClaimValueTypes.String, issuer: "GestorInvetarioEmisor"));
                    //_logger.LogInformation($"Claim añadido en refresh token: permiso={permiso}");
                }
                else
                {
                    //_logger.LogWarning($"Permiso vacío encontrado para el usuario {credencialesUsuario.Id}.");
                }
            }
            // Determinar la estrategia basada en AuthMode en tiempo de ejecución
            string authMode = _configuration["AuthMode"] ?? "Symmetric";
            SigningCredentials signingCredentials;

            switch (authMode)
            {
                case "AsymmetricFixed":
                    // Usar clave fija para el enfoque asimétrico fijo
                    var privateKeyFixed = Environment.GetEnvironmentVariable("PrivateKey") ?? _configuration["JWT:PrivateKey"];
                    if (string.IsNullOrEmpty(privateKeyFixed))
                    {
                        throw new InvalidOperationException("La clave privada no está configurada.");
                    }

                    var rsaFixed = new RSACryptoServiceProvider();
                    rsaFixed.FromXmlString(privateKeyFixed);
                    var rsaSecurityKeyFixed = new RsaSecurityKey(rsaFixed); 
                    signingCredentials = new SigningCredentials(rsaSecurityKeyFixed, SecurityAlgorithms.RsaSha256);
                    break;

                case "AsymmetricDynamic":
                    // Usar claves dinámicas para el enfoque asimétrico dinámico
                    string privateKeyJson;
                    string publicKeyJson;
                    RSAParameters privateKey;
                    RSAParameters publicKey;

                    bool useRedis = _connectionMultiplexer != null && _connectionMultiplexer.GetDatabase().Ping().Milliseconds >= 0;

                    if (useRedis)
                    {
                        privateKeyJson = await _redis.GetStringAsync(credencialesUsuario.Id.ToString() + "PrivateKeyRefresco");
                        publicKeyJson = await _redis.GetStringAsync(credencialesUsuario.Id.ToString() + "PublicKeyRefresco");
                    }
                    else
                    {
                        _memoryCache.TryGetValue(credencialesUsuario.Id.ToString() + "PrivateKeyRefresco", out privateKeyJson);
                        _memoryCache.TryGetValue(credencialesUsuario.Id.ToString() + "PublicKeyRefresco", out publicKeyJson);
                    }

                    if (string.IsNullOrEmpty(privateKeyJson) || string.IsNullOrEmpty(publicKeyJson))
                    {
                        using (var rsa = new RSACryptoServiceProvider(2048))
                        {
                            privateKey = rsa.ExportParameters(true);
                            publicKey = rsa.ExportParameters(false);

                            privateKeyJson = JsonConvert.SerializeObject(privateKey);
                            publicKeyJson = JsonConvert.SerializeObject(publicKey);

                            if (useRedis)
                            {
                                await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "PrivateKeyRefresco", privateKeyJson, new DistributedCacheEntryOptions
                                {
                                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                                });
                                await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "PublicKeyRefresco",
                                   publicKeyJson,
                                   new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) });
                            }
                            else
                            {
                                _memoryCache.Set(credencialesUsuario.Id.ToString() + "PrivateKeyRefresco", privateKeyJson);
                                _memoryCache.Set(credencialesUsuario.Id.ToString() + "PublicKeyRefresco", publicKeyJson);
                            }
                        }
                    }
                    else
                    {
                        privateKey = JsonConvert.DeserializeObject<RSAParameters>(privateKeyJson);
                        publicKey = JsonConvert.DeserializeObject<RSAParameters>(publicKeyJson);
                    }

                    var rsaSecurityKeyDynamic = new RsaSecurityKey(privateKey)
                    {
                        KeyId = credencialesUsuario.Id.ToString() 
                    };
                    signingCredentials = new SigningCredentials(rsaSecurityKeyDynamic, SecurityAlgorithms.RsaSha256);
                    break;

                case "Symmetric":
                    // Usar clave simétrica para el enfoque simétrico
                    var key = Encoding.UTF8.GetBytes(_configuration["ClaveJWT"]);
                    signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
                    break;

                default:
                    throw new NotSupportedException("Modo de autenticación no soportado para el token de refresco.");
            }

            // Generar el token
            var refreshToken = new JwtSecurityToken(
                issuer: _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(refreshToken);
        }
    }
}

