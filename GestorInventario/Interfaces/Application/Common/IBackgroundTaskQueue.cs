namespace GestorInventario.Interfaces.Application.Common
{
    public interface IBackgroundTaskQueue
    {
        void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem);
    }
}