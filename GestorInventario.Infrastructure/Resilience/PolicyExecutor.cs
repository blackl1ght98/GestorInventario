using GestorInventario.Application.Interfaces.Infrastructure.RetryPolicy;

namespace GestorInventario.Infrastructure.Resilience
{
    public class PolicyExecutor: IPolicyExecutor
    {
        private readonly PolicyHandler _policyHandler;

        public PolicyExecutor(PolicyHandler policyHandler)
        {
            _policyHandler = policyHandler;
        }

        public async Task<T> ExecutePolicyAsync<T>(Func<Task<T>> operation)
        {
            var policy = _policyHandler.GetCombinedPolicyAsync<T>();
            return await policy.ExecuteAsync(operation);
        }
       
        public T ExecutePolicy<T>(Func<T> operation)
        {
            var policy = _policyHandler.GetCombinedPolicy<T>();
            return policy.Execute(operation);
        }

        
    }
}
