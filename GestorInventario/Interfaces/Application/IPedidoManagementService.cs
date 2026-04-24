using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.order;

namespace GestorInventario.Interfaces.Application
{
    public interface IPedidoManagementService
    {
        Task<OperationResult<string>> EliminarPedido(int Id);
        Task<OperationResult<string>> EditarPedido(EditPedidoViewModel model);
         Task<OperationResult<PayPalPaymentDetail>> ObtenerDetallePagoEjecutadoV2(string id);
       
    }
}
