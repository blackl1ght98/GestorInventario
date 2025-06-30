using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPedidoRepository
    {
        IQueryable<Pedido> ObtenerPedidos();
        IQueryable<Pedido> ObtenerPedidoUsuario(int userId);
        Task<(bool, string)> CrearPedido(PedidosViewModel model);
        Task<List<Producto>> ObtenerProductos();
        Task<List<Usuario>> ObtenerUsuarios();
        Task<Pedido> ObtenerPedidoEliminacion(int id);
        Task<(bool, string)> EliminarPedido(int Id);
        Task<HistorialPedido> EliminarHistorialPorId(int id);
        Task<(bool, string)> EliminarHistorialPorIdDefinitivo(int Id);
        Task<Pedido> ObtenerPedidoId(int id);
        Task<(bool, string)> EditarPedido(EditPedidoViewModel model);
        IQueryable<HistorialPedido> ObtenerPedidosHistorial();
        IQueryable<HistorialPedido> ObtenerPedidosHistorialUsuario(int usuarioId);
        Task<HistorialPedido> DetallesHistorial(int id);
        Task<(bool, string)> EliminarHitorial();  
        DateTime? ConvertToDateTime(object value); 
        Task<(PayPalPaymentDetail, bool, string)> ObtenerDetallePagoEjecutadoV2(string id);
        Task<Pedido> ObtenerDetallesPedido(int id);


    }
}
