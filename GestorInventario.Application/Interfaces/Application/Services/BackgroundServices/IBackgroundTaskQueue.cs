namespace GestorInventario.Application.Interfaces.Application.Services.BackgroundServices
{
    public interface IBackgroundTaskQueue
    {
        void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem);
    }
}