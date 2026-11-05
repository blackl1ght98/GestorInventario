namespace GestorInventario.Application.Interfaces.BackgroundServices
{
    public interface IBackgroundTaskQueue
    {
        void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem);
    }
}