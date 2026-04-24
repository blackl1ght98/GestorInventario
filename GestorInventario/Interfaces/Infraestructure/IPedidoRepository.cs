using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.order;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPedidoRepository
    {
        IQueryable<Pedido> ObtenerPedidos();
        IQueryable<Pedido> ObtenerPedidoUsuario(int userId); 
        Task<Pedido> ObtenerPedidoConRembolso(int id);
        Task<OperationResult<Pedido>> ActualizarPedidoAsync(Pedido pedido);
        Task<Pedido> ObtenerPedidoConDetallesAsync(int id);
        Task<Pedido> ObtenerPedidoPorId(int id);
        Task<OperationResult<DetallePedido>> ActualizarDetallePedidoAsync(DetallePedido pedido);
        Task<OperationResult<Pedido>> AgregarPedidoAsync(Pedido pedido);
        Task<OperationResult<DetallePedido>> EliminarDetallePedidoAsync(DetallePedido pedido);
        Task<OperationResult<DetallePedido>> AgregarDetallePedidoAsync(DetallePedido detalle);
        Task<DetallePedido> ObtenerDetallePorIdAsync(int id);
        Task<OperationResult<string>> EliminarPedidoAsync(Pedido pedido);
        Task<Pedido?> ObtenerPedidoEnProcesoUsuarioAsync(int usuarioId);
        Task<OperationResult<Pedido>> ObtenerNumeroPedido(RefundFormViewModel form);





    }
}
