using System.Security.Cryptography;

namespace GestorInventario.Interfaces.Application
{
    public interface IEncryptionService
    {     
       
        byte[] Descifrar(byte[] encryptedData, byte[] privateKeyBytes);
        void HandleDecryptionError(Exception ex);
    }
}
