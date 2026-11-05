namespace GestorInventario.Application.Interfaces.Resilience
{
    public interface IPolicyExecutor
    {
        Task<T> ExecutePolicyAsync<T>(Func<Task<T>> operation);
      
        T ExecutePolicy<T>(Func<T> operation);
    }
}
