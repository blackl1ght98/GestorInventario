using System.Security.Cryptography;

namespace GestorInventario.Interfaces.Application
{
    public interface IEncryptionService
    {     
       
        byte[] Descifrar(byte[] encryptedData, byte[] privateKeyBytes);
        string? DescifrarClavePrivada(string? encryptedBase64);
        void HandleDecryptionError(Exception ex);
    }
}
