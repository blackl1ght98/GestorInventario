using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.Pedidos;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IPedidoManagementService
    {
        Task<OperationResult<string>> EliminarPedido(int Id);
        Task<OperationResult<string>> EditarPedido(EditPedidoViewModel model);
        Task<OperationResult<PayPalPaymentDetail>> SincronizarDetallePagoAsync(string id);
    
        Task<OperationResult<Pedido>> ConfirmarPagoDelPedidoAsync(int usuarioActual, string? captureId, string? total, string? currency, string? orderId);

 
    }
}
