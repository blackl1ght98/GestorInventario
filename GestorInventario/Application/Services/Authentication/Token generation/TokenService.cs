using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using GestorInventario.Interfaces.Application;
using GestorInventario.Application.DTOs.User;

namespace GestorInventario.Application.Services
{
    public class TokenService
    {
     
        private readonly ITokenGenerator _tokenService;
        private readonly IRefreshTokenMethod _refreshTokenMethod;
        public TokenService(ITokenGenerator tokenService, IRefreshTokenMethod refresh)
        {
            
            _tokenService = tokenService;
            _refreshTokenMethod = refresh;
        }
       

        public async Task<LoginResponseDto> GenerarToken(Usuario credencialesUsuario)
        {
            // Generar el token principal
            //var tokenPrincipal = await _tokenService.GenerarTokenAsimetricoDinamico(credencialesUsuario);
            var tokenPrincipal = await _tokenService.GenerateTokenAsync(credencialesUsuario);
            // Generar el token de refresco
            var tokenRefresco = await _refreshTokenMethod.GenerarTokenRefresco(credencialesUsuario);

            // Devolver ambos tokens en la respuesta
            return new LoginResponseDto
            {
                Id = tokenPrincipal.Id,
                Token = tokenPrincipal.Token,
                Rol = tokenPrincipal.Rol,
                RefreshToken = tokenRefresco
            };
        }



    }
}
