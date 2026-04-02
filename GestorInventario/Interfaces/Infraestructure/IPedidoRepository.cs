using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.order;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPedidoRepository
    {
        IQueryable<Pedido> ObtenerPedidos();
        IQueryable<Pedido> ObtenerPedidoUsuario(int userId);
        Task<OperationResult<string>> CrearPedido(PedidosViewModel model);      
        Task<Pedido> ObtenerPedidoEliminacion(int id);
        Task<OperationResult<string>> EliminarPedido(int Id);

        Task<Pedido> ObtenerPedidoPorId(int id);
        Task<OperationResult<string>> EditarPedido(EditPedidoViewModel model);
       
      

        Task<OperationResult<PayPalPaymentDetail>> ObtenerDetallePagoEjecutadoV2(string id);
        Task<Pedido> ObtenerDetallesPedido(int id);
      
    



    }
}
