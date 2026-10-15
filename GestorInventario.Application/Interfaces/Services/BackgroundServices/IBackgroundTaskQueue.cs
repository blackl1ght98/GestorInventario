namespace GestorInventario.Application.Interfaces.Services.BackgroundServices
{
    public interface IBackgroundTaskQueue
    {
        void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem);
    }
}