namespace GestorInventario.Interfaces.Application.Services.Background
{
    public interface IBackgroundTaskQueue
    {
        void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem);
    }
}