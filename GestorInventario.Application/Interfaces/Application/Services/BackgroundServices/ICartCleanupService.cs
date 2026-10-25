namespace GestorInventario.Application.Interfaces.Application.Services.BackgroundServices;

public interface ICartCleanupService
{
    Task<int> CleanupAbandonedCartsAsync(CancellationToken ct);
}