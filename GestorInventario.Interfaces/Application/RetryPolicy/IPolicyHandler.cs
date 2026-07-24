using Polly;

namespace GestorInventario.Interfaces.Application.RetryPolicy
{
    public interface IPolicyHandler
    {
        IAsyncPolicy<T> GetCombinedPolicyAsync<T>();
       
        Policy<T> GetCombinedPolicy<T>();
    }
}
