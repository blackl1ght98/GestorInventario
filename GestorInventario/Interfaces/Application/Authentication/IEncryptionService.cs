namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface IEncryptionService
    {     
       
        byte[] Descifrar(byte[] encryptedData, byte[] privateKeyBytes);
        string? DescifrarClavePrivada(string? encryptedBase64);
        string EncryptPrivateKey(string privateKeyJson);
        void HandleDecryptionError(Exception ex);
    }
}
