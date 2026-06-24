namespace GestorInventario.Interfaces.Application.Services
{
    public interface IStockCheckService
    {
        Task VerificarYNotificarStockBajoAsync(CancellationToken stoppingToken = default);
    }
}
