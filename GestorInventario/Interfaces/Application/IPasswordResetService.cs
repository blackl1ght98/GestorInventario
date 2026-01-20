namespace GestorInventario.Interfaces.Application
{
    public interface IPasswordResetService
    {
        Task<(bool Success, string Error, string Email)> EnviarCorreoResetAsync(string email);
    }
}
