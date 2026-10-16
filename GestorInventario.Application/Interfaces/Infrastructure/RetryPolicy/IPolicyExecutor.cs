namespace GestorInventario.Application.Interfaces.Infrastructure.RetryPolicy
{
    public interface IPolicyExecutor
    {
        Task<T> ExecutePolicyAsync<T>(Func<Task<T>> operation);
      
        T ExecutePolicy<T>(Func<T> operation);
    }
}
