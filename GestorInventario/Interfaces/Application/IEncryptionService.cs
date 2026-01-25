using System.Security.Cryptography;

namespace GestorInventario.Interfaces.Application
{
    public interface IEncryptionService
    {     
        byte[] Descifrar(byte[] data, byte[] aesKey);
        byte[] DescifrarV1(byte[] encryptedData, byte[] privateKeyBytes);
        void HandleDecryptionError(Exception ex);
    }
}
