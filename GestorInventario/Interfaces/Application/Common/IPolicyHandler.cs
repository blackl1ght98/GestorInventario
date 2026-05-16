using Polly;

namespace GestorInventario.Interfaces.Application.Common
{
    public interface IPolicyHandler
    {
        IAsyncPolicy<T> GetCombinedPolicyAsync<T>();
        IAsyncPolicy GetCombinedPolicyAsync();
        Policy<T> GetCombinedPolicy<T>();
    }
}
