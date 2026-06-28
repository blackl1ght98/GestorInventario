namespace GestorInventario.Interfaces.Application.Common
{
    public interface ICacheService
    {
        Task SetStringAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetStringAsync(string key);
        void SetLocal(string key, string value);
        string? GetLocal(string key);
        Task RemoveAsync(string key);
    }
}
