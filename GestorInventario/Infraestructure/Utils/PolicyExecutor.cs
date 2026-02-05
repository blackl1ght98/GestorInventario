using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Interfaces.Application;

namespace GestorInventario.Infraestructure.Utils
{
    public class PolicyExecutor: IPolicyExecutor
    {
        private readonly IPolicyHandler _policyHandler;

        public PolicyExecutor(IPolicyHandler policyHandler)
        {
            _policyHandler = policyHandler;
        }

        public async Task<T> ExecutePolicyAsync<T>(Func<Task<T>> operation)
        {
            var policy = _policyHandler.GetCombinedPolicyAsync<T>();
            return await policy.ExecuteAsync(operation);
        }
        public async Task ExecutePolicyAsync(Func<Task> operation)
        {
            var policy = _policyHandler.GetCombinedPolicyAsync();
            await policy.ExecuteAsync(operation);
        }
        public T ExecutePolicy<T>(Func<T> operation)
        {
            var policy = _policyHandler.GetCombinedPolicy<T>();
            return policy.Execute(operation);
        }

        
    }
}
