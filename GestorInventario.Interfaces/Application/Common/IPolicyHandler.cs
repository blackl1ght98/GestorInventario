using Polly;

namespace GestorInventario.Interfaces.Application.Common
{
    public interface IPolicyHandler
    {
        IAsyncPolicy<T> GetCombinedPolicyAsync<T>();
       
        Policy<T> GetCombinedPolicy<T>();
    }
}
