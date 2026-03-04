namespace GestorInventario.Interfaces.Application
{
    public interface IPayPalHttpClient
    {
        Task<string> GetAccessTokenAsync(string clientId, string clientSecret);
        // Sobrecarga principal (la más completa)
        Task<T> ExecutePayPalRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            object? content = null,
            string? rawJsonBody = null,
            Func<HttpResponseMessage, Task>? onError = null);

        // Sobrecarga para content + onError (la que más vas a usar)
        Task<T> ExecutePayPalRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            object content,
            Func<HttpResponseMessage, Task>? onError = null);

        // Sobrecarga para rawJsonBody + onError
        Task<T> ExecutePayPalRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            string rawJsonBody,
            Func<HttpResponseMessage, Task>? onError = null);

        // Sobrecarga para GET/DELETE sin body
        Task<T> ExecutePayPalRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            Func<HttpResponseMessage, Task>? onError = null);

    }
}
