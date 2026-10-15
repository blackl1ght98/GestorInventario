namespace GestorInventario.Application.Interfaces.Services.BackgroundServices
{
    public interface IStockCheckService
    {
        Task VerificarYNotificarStockBajoAsync(CancellationToken stoppingToken = default);
    }
}
