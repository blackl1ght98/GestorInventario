using GestorInventario.Interfaces.Application.Common;
using System.Threading.Channels;

namespace GestorInventario.Utilities
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<BackgroundTaskQueue> _logger;
        public BackgroundTaskQueue(IServiceScopeFactory serviceScopeFactory, ILogger<BackgroundTaskQueue> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _queue = Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>();

            _ = ProcessQueueAsync();
        }

        public void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem)
        {
            _queue.Writer.TryWrite(workItem);
        }

        private async Task ProcessQueueAsync()
        {
            await foreach (var workItem in _queue.Reader.ReadAllAsync())
            {
                using var scope = _serviceScopeFactory.CreateScope();
                try
                {
                    await workItem(scope.ServiceProvider, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error al ejecutar la tarea en segundo plano ", ex.Message);
                }
            }
        }
    }
}