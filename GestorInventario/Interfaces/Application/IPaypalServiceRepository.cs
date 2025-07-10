using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface IPaypalServiceRepository
    {
    
        Task UpdatePlanStatusInDatabase(string planId, string status);
        Task<(Pedido Pedido, decimal TotalAmount)> GetPedidoWithDetailsAsync(int pedidoId);
        Task UpdatePedidoStatusAsync(int pedidoId, string status);
        Task SavePlanDetailsAsync(string planId, PaypalPlanDetailsDto planDetails);
    }
}
