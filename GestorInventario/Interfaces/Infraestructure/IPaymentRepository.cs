using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaymentRepository
    {
       
       
        OperationResult<PayPalPaymentDetail> ProcesarDetallesSuscripcion(OrderDetailsResponse detallespago);
        Task<OperationResult<PayPalPaymentItem>> ProcesarRembolso(PurchaseUnitDetails firstPurchaseUnit, PayPalPaymentDetail detallesSuscripcion, int usuarioActual, RefundFormViewModel form, Pedido obtenerNumeroPedido, string emailCliente);
        Task LimpiarPedidoCorruptoUsuarioAsync(int userId);
        Task<OperationResult<PayPalPaymentDetail>> AgregarDetallePagoAsync(PayPalPaymentDetail detalle);
        Task<OperationResult<PayPalPaymentItem>> AgregarPagoItemAsync(PayPalPaymentItem detalle);
        Task<PayPalPaymentDetail> ObtenerDetallesPago(string id);
        Task<OperationResult<string>> EliminarDetallesPagoAsync(PayPalPaymentDetail pago);
    }
}
