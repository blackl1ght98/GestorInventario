using Polly;

namespace GestorInventario.Interfaces.Application
{
    public interface IPolicyHandler
    {
        IAsyncPolicy<T> GetCombinedPolicyAsync<T>();
        IAsyncPolicy GetCombinedPolicyAsync();
        Policy<T> GetCombinedPolicy<T>();
    }
}
