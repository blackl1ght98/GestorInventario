using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Infraestructure.Repositories
{
    public interface IPedidoRepository
    {
        //Consultas
        IQueryable<Pedido> ObtenerPedidos();
        IQueryable<Pedido> ObtenerPedidoUsuario(int userId);     
        Task<Pedido> ObtenerPedidoConDetallesAsync(int id);
        Task<Pedido> ObtenerPedidoPorIdAsync(int id);
        Task<Pedido?> ObtenerPedidoPendienteUsuarioAsync(int usuarioId);
        Task<Pedido> ObtenerNumeroPedido(RefundDto form);
        Task<List<DetallePedido>> ObtenerDetallesPedidoAsync(int pedidoId);
        Task<DetallePedido> ObtenerDetalleParaReembolsoAsync(int detallePedidoId);
        Task<DetallePedido> ObtenerDetallePorIdAsync(int id);
        Task<Pedido> ObtenerPedidoConCapturasAsync(int id);
        //Operaciones
        Task<OperationResult<Pedido>> ActualizarPedidoAsync(Pedido pedido);
        Task<OperationResult<DetallePedido>> ActualizarDetallePedidoAsync(DetallePedido pedido);
        Task<OperationResult<Pedido>> AgregarPedidoAsync(Pedido pedido);
        Task<OperationResult<DetallePedido>> EliminarDetallePedidoAsync(DetallePedido pedido);
        Task<OperationResult<DetallePedido>> AgregarDetallePedidoAsync(DetallePedido detalle); 
        Task<OperationResult<string>> EliminarCarritoAsync(Pedido pedido);
     


    }
}
