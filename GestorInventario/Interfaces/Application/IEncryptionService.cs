using System.Security.Cryptography;

namespace GestorInventario.Interfaces.Application
{
    public interface IEncryptionService
    {     
       
        byte[] Descifrar(byte[] encryptedData, byte[] privateKeyBytes);
        string? DescifrarClavePrivada(string? encryptedBase64);
        string EncryptPrivateKey(string privateKeyJson);
        void HandleDecryptionError(Exception ex);
    }
}
