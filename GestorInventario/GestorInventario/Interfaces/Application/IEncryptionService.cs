using System.Security.Cryptography;

namespace GestorInventario.Interfaces.Application
{
    public interface IEncryptionService
    {
        byte[] Cifrar(byte[] data, byte[] aesKey);
        byte[] Descifrar(byte[] data, byte[] aesKey);
        byte[] Descifrar(byte[] encryptedData, RSAParameters privateKeyParams);
        void HandleDecryptionError(Exception ex);
    }
}
