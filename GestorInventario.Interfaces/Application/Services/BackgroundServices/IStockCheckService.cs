namespace GestorInventario.Interfaces.Application.Services.BackgroundServices
{
    public interface IStockCheckService
    {
        Task VerificarYNotificarStockBajoAsync(CancellationToken stoppingToken = default);
    }
}
