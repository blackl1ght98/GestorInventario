namespace GestorInventario.Application.Interfaces.BackgroundServices
{
    public interface IStockCheckService
    {
        Task VerificarYNotificarStockBajoAsync(CancellationToken stoppingToken = default);
    }
}
