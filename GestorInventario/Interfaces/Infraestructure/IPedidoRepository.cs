using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.order;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPedidoRepository
    {
        IQueryable<Pedido> ObtenerPedidos();
        IQueryable<Pedido> ObtenerPedidoUsuario(int userId);
        Task<PayPalPaymentDetail> ObtenerDetallesPago(string id);
        Task<OperationResult<PayPalPaymentItem>> AgregarPagoItemAsync(PayPalPaymentItem detalle);
        Task<OperationResult<string>> EliminarDetallesPagoAsync(PayPalPaymentDetail pago);
        Task<OperationResult<PayPalPaymentDetail>> AgregarDetallePagoAsync(PayPalPaymentDetail detalle);
        Task<Pedido> ObtenerPedidoConRembolso(int id);
        Task<OperationResult<Pedido>> ActualizarPedidoAsync(Pedido pedido);
        Task<Pedido> ObtenerPedidoConDetallesAsync(int id);
        Task<Pedido> ObtenerPedidoPorId(int id);
      
        Task<OperationResult<Pedido>> AgregarPedidoAsync(Pedido pedido);

        Task<OperationResult<DetallePedido>> AgregarDetallePedidoAsync(DetallePedido detalle);

        Task<OperationResult<string>> EliminarPedidoAsync(Pedido pedido);
      //  Task<OperationResult<PayPalPaymentDetail>> ObtenerDetallePagoEjecutadoV2(string id);
       
      
    



    }
}
