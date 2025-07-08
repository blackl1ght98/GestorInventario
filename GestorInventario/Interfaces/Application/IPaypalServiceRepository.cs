using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface IPaypalServiceRepository
    {
        Task SavePlanDetailsToDatabase(string createdPlanId, dynamic planRequest);
        Task UpdatePlanStatusInDatabase(string planId, string status);
        Task<(Pedido Pedido, decimal TotalAmount)> GetPedidoWithDetailsAsync(int pedidoId);
        Task UpdatePedidoStatusAsync(int pedidoId, string status);
    }
}
