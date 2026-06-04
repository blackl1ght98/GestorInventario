using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure.Repositories
{
    public interface IPedidoRepository
    {
        //Consultas
        IQueryable<Pedido> ObtenerPedidos();
        IQueryable<Pedido> ObtenerPedidoUsuario(int userId); 
        Task<Pedido> ObtenerPedidoConRembolso(int id);      
        Task<Pedido> ObtenerPedidoConDetallesAsync(int id);
        Task<Pedido> ObtenerPedidoPorIdAsync(int id);
        Task<Pedido?> ObtenerPedidoPendienteUsuarioAsync(int usuarioId);
        Task<Pedido> ObtenerNumeroPedido(RefundFormViewModel form);
        Task<List<DetallePedido>> ObtenerDetallesPedidoAsync(int pedidoId);
        //Excepciones en consultas uso de OperationResult por complejidad
        Task<OperationResult<(string captureId, string currency, decimal subtotal, decimal iva, decimal total, int pedidoId, string numeroPedido)>>
     GetPedidoWithDetailsAsync(int pedidoId);
        Task<OperationResult<(int idPedido, string captureId, decimal precioProducto, string paymentId, string currency, int detalleId)>> GetProductoDePedidoAsync(int detallePedidoId);
      
        Task<DetallePedido> ObtenerDetallePorIdAsync(int id);
        //Operaciones
        Task<OperationResult<Pedido>> ActualizarPedidoAsync(Pedido pedido);
        Task<OperationResult<DetallePedido>> ActualizarDetallePedidoAsync(DetallePedido pedido);
        Task<OperationResult<Pedido>> AgregarPedidoAsync(Pedido pedido);
        Task<OperationResult<DetallePedido>> EliminarDetallePedidoAsync(DetallePedido pedido);
        Task<OperationResult<DetallePedido>> AgregarDetallePedidoAsync(DetallePedido detalle); 
        Task<OperationResult<string>> EliminarPedidoAsync(Pedido pedido);
     


    }
}
