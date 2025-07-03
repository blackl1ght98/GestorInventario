using GestorInventario.Application.Politicas_Resilencia;

namespace GestorInventario.Infraestructure.Utils
{
    public class PolicyExecutor
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
        private async Task ExecutePolicyAsync(Func<Task> operation)
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
