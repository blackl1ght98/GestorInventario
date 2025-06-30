using System.Security.Cryptography;

namespace GestorInventario.Interfaces.Application
{
    public interface IEncryptionService
    {
      
        byte[] Descifrar(byte[] data, byte[] aesKey);
        byte[] Descifrar(byte[] encryptedData, RSAParameters privateKeyParams);
        void HandleDecryptionError(Exception ex);
    }
}
