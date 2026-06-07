namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface IEncryptionService
    {     
       
       
        string? DescifrarClavePrivada(string? encryptedBase64);
        string EncryptPrivateKey(string privateKeyJson);
        void HandleDecryptionError(Exception ex);
    }
}
