using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPedidoRepository
    {
        //Consultas
        IQueryable<Pedido> ObtenerPedidos();
        IQueryable<Pedido> ObtenerPedidoUsuario(int userId); 
        Task<Pedido> ObtenerPedidoConRembolso(int id);      
        Task<Pedido> ObtenerPedidoConDetallesAsync(int id);
        Task<Pedido> ObtenerPedidoPorIdAsync(int id);
        Task<Pedido?> ObtenerPedidoEnProcesoUsuarioAsync(int usuarioId);
        Task<Pedido> ObtenerNumeroPedido(RefundFormViewModel form);
        //Excepciones en consultas uso de OperationResult por complejidad
        Task<OperationResult<(Pedido, decimal)>> GetPedidoWithDetailsAsync(int pedidoId);
        Task<OperationResult<(DetallePedido, decimal)>> GetProductoDePedidoAsync(int detallePedidoId);
        Task<OperationResult<(Pedido, List<DetallePedido>)>> GetPedidoConDetallesAsync(int pedidoId);
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
