using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using System.Security.Cryptography;

namespace GestorInventario.Interfaces.Application
{
    public interface ITokenGenerator
    {
        Task<DTOLoginResponse> GenerarTokenSimetrico(Usuario credencialesUsuario);
        Task<DTOLoginResponse> GenerarTokenAsimetricoFijo(Usuario credencialesUsuario);
        Task<DTOLoginResponse> GenerarTokenAsimetricoDinamico(Usuario credencialesUsuario);
       
        byte[] Cifrar(byte[] data, byte[] aesKey);
        byte[] Descifrar(byte[] encryptedData, RSAParameters privateKeyParams);
        byte[] Descifrar(byte[] data, byte[] aesKey);
        void HandleDecryptionError(Exception ex);
        Task<string> GenerarTokenRefresco(Usuario credencialesUsuario);


    }
}
