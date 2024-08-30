using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestorInventario.Domain.Models;
using GestorInventario.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using GestorInventario.Interfaces.Application;

namespace GestorInventario.Application.Services
{
    public class TokenService
    {
     
        private readonly ITokenGenerator _tokenService;
        public TokenService(ITokenGenerator tokenService)
        {
            
            _tokenService = tokenService;
        }
        //public async Task<DTOLoginResponse> GenerarToken(Usuario credencialesUsuario)
        //{
        //    return await _tokenService.GenerarTokenSimetrico(credencialesUsuario);
        //}

        //public async Task<DTOLoginResponse> GenerarToken(Usuario credencialesUsuario)
        //{
        //    return await _tokenService.GenerarTokenAsimetricoFijo(credencialesUsuario);
        //}

        public async Task<DTOLoginResponse> GenerarToken(Usuario credencialesUsuario)
        {
            return await _tokenService.GenerarTokenAsimetricoDinamico(credencialesUsuario);
        }


    }
}
