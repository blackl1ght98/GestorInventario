using Polly;

namespace GestorInventario.Application.Interfaces.RetryPolicy
{
    public interface IPolicyHandler
    {
        IAsyncPolicy<T> GetCombinedPolicyAsync<T>();
       
        Policy<T> GetCombinedPolicy<T>();
    }
}
