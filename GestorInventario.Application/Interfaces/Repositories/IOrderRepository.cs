using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Repositories
{
    public interface IOrderRepository
    {
        //Consultas
        IQueryable<Pedidos> ObtenerPedidos();
        IQueryable<Pedidos> ObtenerPedidoUsuario(int userId);     
        Task<Pedidos> ObtenerPedidoConDetallesAsync(int id);
        Task<Pedidos> ObtenerPedidoPorIdAsync(int id);
        Task<Pedidos?> ObtenerPedidoPendienteUsuarioAsync(int usuarioId);
        Task<Pedidos> ObtenerNumeroPedido(string numPedido);
        Task<List<DetallePedido>> ObtenerDetallesPedidoAsync(int pedidoId);
        Task<DetallePedido> ObtenerDetalleParaReembolsoAsync(int detallePedidoId);
        Task<DetallePedido> ObtenerDetallePorIdAsync(int id);
        Task<Pedidos> ObtenerPedidoConCapturasAsync(int id);

        Task<List<Pedidos>> ObtenerTodosLosPendientesAntiguosAsync(DateTime limiteTiempo);
        //Operaciones
        Task<OperationResult<Pedidos>> ActualizarPedidoAsync(Pedidos pedido);
        Task<OperationResult<DetallePedido>> ActualizarDetallePedidoAsync(DetallePedido pedido);
        Task<OperationResult<Pedidos>> AgregarPedidoAsync(Pedidos pedido);
        Task<OperationResult<DetallePedido>> EliminarDetallePedidoAsync(DetallePedido pedido);
        Task<OperationResult<DetallePedido>> AgregarDetallePedidoAsync(DetallePedido detalle); 
        Task<OperationResult<string>> EliminarCarritoAsync(Pedidos pedido);
     


    }
}
