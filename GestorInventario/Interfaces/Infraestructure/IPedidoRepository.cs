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
        Task<List<Producto>> ObtenerProductos();
        Task<List<Usuario>> ObtenerUsuarios();
        Task<Pedido> ObtenerPedidoEliminacion(int id);
        Task<OperationResult<string>> EliminarPedido(int Id);
        Task<HistorialPedido> EliminarHistorialPorId(int id);
        Task<OperationResult<string>> EliminarHistorialPorIdDefinitivo(int Id);
        Task<Pedido> ObtenerPedidoId(int id);
        Task<OperationResult<string>> EditarPedido(EditPedidoViewModel model);
        IQueryable<HistorialPedido> ObtenerPedidosHistorial();
        IQueryable<HistorialPedido> ObtenerPedidosHistorialUsuario(int usuarioId);
        Task<HistorialPedido> DetallesHistorial(int id);
        Task<OperationResult<string>> EliminarHitorial();  
        DateTime? ConvertToDateTime(object value);
        Task<OperationResult<PayPalPaymentDetail>> ObtenerDetallePagoEjecutadoV2(string id);
        Task<Pedido> ObtenerDetallesPedido(int id);


    }
}
