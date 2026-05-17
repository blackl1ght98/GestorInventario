namespace GestorInventario.Interfaces.Application.Services
{
    public interface IStockNotificationService
    {
        Task VerificarYNotificarStockBajoAsync(CancellationToken stoppingToken = default);
    }
}
