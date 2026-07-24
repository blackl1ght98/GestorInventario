namespace GestorInventario.Interfaces.Application.Services.Background
{
    public interface IStockCheckService
    {
        Task VerificarYNotificarStockBajoAsync(CancellationToken stoppingToken = default);
    }
}
