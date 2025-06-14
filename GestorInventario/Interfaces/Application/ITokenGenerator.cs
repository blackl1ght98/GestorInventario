using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using System.Security.Cryptography;

namespace GestorInventario.Interfaces.Application
{
    public interface ITokenGenerator
    {
 
        Task<DTOLoginResponse> GenerateTokenAsync(Usuario credencialesUsuario);
       
      


    }
}
