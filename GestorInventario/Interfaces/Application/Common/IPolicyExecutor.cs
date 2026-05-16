namespace GestorInventario.Interfaces.Application.Common
{
    public interface IPolicyExecutor
    {
        Task<T> ExecutePolicyAsync<T>(Func<Task<T>> operation);
        Task ExecutePolicyAsync(Func<Task> operation);
        T ExecutePolicy<T>(Func<T> operation);
    }
}
