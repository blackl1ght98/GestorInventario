using GestorInventario.Application.Mappers;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Orders;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Web;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.Orders
{
    public class OrderService: IOrderService
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly ILogger<OrderService> _logger;
       
       
        private readonly IPaypalOrderService _paypalOrderService;
        private readonly IPaymentRepository _paymentRepository;
      
        public OrderService(ILogger<OrderService> logger,  IPedidoRepository pedido, 
           IPaypalOrderService paypal, IPaymentRepository payment)
        {
            
            _logger = logger;
            _pedidoRepository = pedido;
           
       
            _paypalOrderService = paypal;
            _paymentRepository = payment;
           
        
        }
        public async Task<OperationResult<string>> EliminarPedido(int id)
        {
            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(id);
            if (pedido == null)
                return OperationResult<string>.Fail("Pedido no encontrado");

            // Solo carritos sin capturas de PayPal
            if (pedido.EstadoPedido == EstadoPedido.Carrito.ToString()
                && !pedido.PayPalPaymentCaptures.Any())
            {
                await _pedidoRepository.EliminarCarritoAsync(pedido);
                return OperationResult<string>.Ok("Carrito eliminado");
            }

            return OperationResult<string>.Fail("No se puede eliminar un pedido con historial");
        }

        public async Task<OperationResult<PayPalPaymentDetail>> SincronizarDetallePagoAsync(
           string id, int pedidoId)
        {
            var detallesPayPal = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(id);
            if (detallesPayPal == null)
                return OperationResult<PayPalPaymentDetail>.Fail(
                    "Detalles del pedido no encontrados para generar la factura");

            var detalleBD = await ObtenerOCrearDetallePagoAsync(id, detallesPayPal);
            PayPalPaymentMapper.MapearPayer(detallesPayPal, detalleBD);

            var primeraUnidad = detallesPayPal.PurchaseUnits?.FirstOrDefault();
            if (primeraUnidad != null)
            {
                await ProcesarUnidadDeCompraAsync(primeraUnidad, detalleBD, pedidoId);
            }

            return OperationResult<PayPalPaymentDetail>.Ok("", detalleBD);
        }


        /// <summary>
        /// Busca el detalle en BD. Si no existe, lo crea.
        /// Si existe, lo elimina y reutiliza la instancia .
        /// </summary>
        private async Task<PayPalPaymentDetail> ObtenerOCrearDetallePagoAsync(
            string id, OrderDetailsResponse detallesPayPal)
        {
            var existente = await _paymentRepository.ObtenerDetallesPago(id);

            if (existente == null)
            {
                var nuevo = new PayPalPaymentDetail { Id = detallesPayPal.Id };
                await _paymentRepository.AgregarDetallePagoAsync(nuevo);
                _logger.LogInformation("Detalle de pago {Id} creado en BD", id);
                return nuevo;
            }

            await _paymentRepository.EliminarDetallesPagoAsync(existente);
            _logger.LogInformation("Detalle de pago {Id} actualizado en BD", id);
            return existente;
        }

   

        /// <summary>
        /// Procesa una unidad de compra: shipping, montos, captures, refunds e items.
        /// </summary>
        private async Task ProcesarUnidadDeCompraAsync(
            PurchaseUnitDetails unidad,
            PayPalPaymentDetail detallePago,
            int pedidoId)
        {
            await ProcesarShippingAsync(unidad, detallePago);
            PayPalPaymentMapper.MapearMontos(unidad, detallePago);
            await ProcesarCapturesAsync(unidad, detallePago, pedidoId);
            await ProcesarRefundsAsync(unidad, detallePago, pedidoId);
            await ProcesarItemsAsync(unidad, detallePago);
        }

        /// <summary>
        /// Crea el registro de información de envío.
        /// </summary>
        private async Task ProcesarShippingAsync(
            PurchaseUnitDetails unidad, PayPalPaymentDetail detallePago)
        {
            var shipping = unidad.Shipping;
            var envio = new PayPalPaymentShipping
            {
                PaymentId = detallePago.Id,
                RecipientName = shipping.Name.FullName,
                AddressLine1 = shipping.Address.AddressLine1,
                City = shipping.Address.AdminArea2,
                State = shipping.Address.AdminArea1,
                PostalCode = shipping.Address.PostalCode,
                CountryCode = shipping.Address.CountryCode
            };

            await _paymentRepository.AgregarInfoEnvioAsync(envio);
        }



        /// <summary>
        /// Procesa las capturas de pago (captures) de PayPal.
        /// </summary>
        private async Task ProcesarCapturesAsync(
            PurchaseUnitDetails unidad,
            PayPalPaymentDetail detallePago,
            int pedidoId)
        {
            if (unidad.Payments?.Captures == null)
                return;

            foreach (var capture in unidad.Payments.Captures.Where(c => c != null))
            {
                var paypalCapture = PayPalPaymentMapper.MapearCapture(capture, detallePago.Id, pedidoId);
                await _paymentRepository.AgregarCaptureAsync(paypalCapture);
            }
        }

     

        /// <summary>
        /// Procesa los reembolsos (refunds) de PayPal.
        /// </summary>
        private async Task ProcesarRefundsAsync(
            PurchaseUnitDetails unidad,
            PayPalPaymentDetail detallePago,
            int pedidoId)
        {
            if (unidad.Payments?.Refunds == null)
                return;

            foreach (var refund in unidad.Payments.Refunds.Where(r => r != null))
            {
                var paypalRefund = PayPalPaymentMapper.MapearRefund(refund, detallePago.Id, pedidoId);
                await _paymentRepository.AgregarRefundAsync(paypalRefund);
            }
        }

 

        /// <summary>
        /// Procesa los items del pedido.
        /// </summary>
        private async Task ProcesarItemsAsync(
            PurchaseUnitDetails unidad, PayPalPaymentDetail detallePago)
        {
            if (unidad.Items == null)
                return;

            foreach (var item in unidad.Items)
            {
                var paymentItem = new PayPalPaymentItem
                {
                    PayPalId = detallePago.Id,
                    ItemName = item.Name,
                    ItemSku = item.Sku,
                    ItemPrice = ConversionExtensions.ToDecimalSafe(item.UnitAmount.Value),
                    ItemCurrency = item.UnitAmount.CurrencyCode,
                    ItemTax = ConversionExtensions.ToDecimalSafe(item.Tax.Value),
                    ItemQuantity = ConversionExtensions.ToIntSafe(item.Quantity)
                };

                await _paymentRepository.AgregarPagoItemAsync(paymentItem);
            }
        }

    


        public async Task<OperationResult<Pedido>> ConfirmarPagoDelPedidoAsync(
           int usuarioActual,
           string captureId,
           decimal total,
           string? currency,
           string orderId)
        {
           
            if (string.IsNullOrWhiteSpace(captureId) ||
                string.IsNullOrWhiteSpace(currency) ||
                string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogWarning("Parámetros inválidos al confirmar pago: captureId, currency u orderId vacíos");
                return OperationResult<Pedido>.Fail("Datos no validos");
            }

          
            var pedido = await _pedidoRepository.ObtenerPedidoPendienteUsuarioAsync(usuarioActual);
            if (pedido == null)
            {
                _logger.LogWarning("No se encontró pedido pendiente para el usuario {UsuarioId}", usuarioActual);
                return OperationResult<Pedido>.Fail("Pedido no encontrado");
            }

           
            var paymentDetail = await _paymentRepository.ObtenerDetallesPago(orderId);

            if (paymentDetail == null)
            {
                paymentDetail = new PayPalPaymentDetail
                {
                    Id = orderId,
                    Intent = "CAPTURE",          
                    AmountTotal = total,
                    AmountCurrency = currency,
                    AmountItemTotal = total,
                    AmountShipping = 0,
                    Description = $"Pedido #{pedido.NumeroPedido} - pendiente de sincronización con PayPal",
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow
                };
                await _paymentRepository.AgregarDetallePagoAsync(paymentDetail);
            }


            var capturePayment = new PayPalPaymentCapture
            {
                PaymentId = orderId,
                CaptureId = captureId,
                PedidoId = pedido.Id,
                Amount = total,
                Currency = currency,   
                TransactionFeeAmount = 0, 
                ReceivableAmount = 0,     
                ExchangeRate = 0,
                FinalCapture = false,
                CreateTime = DateTime.UtcNow,
                UpdateTime = DateTime.UtcNow
            };

            await _paymentRepository.AgregarCaptureAsync(capturePayment);

           
            pedido.Total = total;
            pedido.Currency = currency;
            pedido.EstadoPedido = EstadoPedido.Pagado.ToString();

            await _pedidoRepository.ActualizarPedidoAsync(pedido);

            return OperationResult<Pedido>.Ok("Pago confirmado. Pendiente de sincronización con PayPal.", pedido);
        }
      
        public async Task AddInfoTrackingOrder(int pedidoId, string tracking, string carrier)
        {

            var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);
            if (pedido == null)
                throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");
            pedido.EstadoPedido = EstadoPedido.Enviado.ToString();
            pedido.TrackingNumber = tracking;
            pedido.UrlTracking = "URL NO ESPECIFICADA";
            pedido.Transportista = carrier;
            await _pedidoRepository.ActualizarPedidoAsync(pedido);


        }

    }
}
