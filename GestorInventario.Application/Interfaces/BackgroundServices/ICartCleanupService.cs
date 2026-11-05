namespace GestorInventario.Application.Interfaces.BackgroundServices;

public interface ICartCleanupService
{
    Task<int> CleanupAbandonedCartsAsync(CancellationToken ct);
}