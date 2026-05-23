
using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Repositories.PaypalRepository;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.ViewModels.Pedidos;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class PedidoManagementService: IPedidoManagementService
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly ILogger<PedidoManagementService> _logger;
        private readonly ICurrentUserAccessor _currentUserAccesor;
        private readonly IConversionUtils _conversion;
        private readonly IPaypalOrderService _paypalOrder;
        private readonly IPaymentRepository _payment;
        private readonly IPaypalRepository _paypalRepository;
        public PedidoManagementService(ILogger<PedidoManagementService> logger,  IPedidoRepository pedido, ICurrentUserAccessor current,
            IConversionUtils conversion, IPaypalOrderService paypal, IPaymentRepository payment, IPaypalRepository paypalRepository)
        {
            
            _logger = logger;
            _pedidoRepository = pedido;
            _currentUserAccesor = current;
            _conversion = conversion;
            _paypalOrder = paypal;
            _payment = payment;
            _paypalRepository=paypalRepository;
        
        }
        public async Task<OperationResult<string>> EliminarPedido(int Id)
        {
          
                var pedido = await _pedidoRepository.ObtenerPedidoConRembolso(Id);
                if (pedido == null)
                {
                    return OperationResult<string>.Fail("No hay pedido para eliminar");
                }
            if (pedido.EstadoPedido != EstadoPedido.Carrito.ToString())
            {
                await _pedidoRepository.EliminarPedidoAsync(pedido);
                return OperationResult<string>.Ok("Pedido eliminado con exito");
            }
            if (pedido.EstadoPedido != EstadoPedido.Entregado.ToString())
            {
                return OperationResult<string>.Fail("El pedido tiene que tener el estado Entregado para ser eliminado y no tener historial asociado");
            }
          
            await _pedidoRepository.EliminarPedidoAsync(pedido);
                return OperationResult<string>.Ok("Pedido eliminado con exito");
        }
        public async Task<OperationResult<string>> EditarPedido(EditPedidoViewModel model)
        {
            

                int usuarioId = _currentUserAccesor.GetCurrentUserId();
                var pedidoOriginal = await _pedidoRepository.ObtenerPedidoConDetallesAsync(model.Id);
                if (pedidoOriginal == null)
                {
                    return OperationResult<string>.Fail("Pedido no encontrado");
                }
                pedidoOriginal.FechaPedido = model.FechaPedido;
                pedidoOriginal.EstadoPedido = model.EstadoPedido;
                await _pedidoRepository.ActualizarPedidoAsync(pedidoOriginal);
                return OperationResult<string>.Ok("Pedido editado con exito");
      


        }
        public async Task<OperationResult<PayPalPaymentDetail>> SincronizarDetallePagoAsync(string id, int pedidoId)
        {
            var detalles = await _paypalOrder.ObtenerDetallesPagoEjecutadoAsync(id);
            if (detalles == null)
                return OperationResult<PayPalPaymentDetail>.Fail("Detalles del pedido no encontrados para generar la factura");

            var existingDetail = await _payment.ObtenerDetallesPago(id);
            PayPalPaymentDetail detallesPago;

            if (existingDetail == null)
            {
                // Crear nuevo registro
                detallesPago = new PayPalPaymentDetail { Id = detalles.Id };
                await _payment.AgregarDetallePagoAsync(detallesPago);
                _logger.LogInformation("Detalle de pago {Id} creado en BD", id);
            }
            else
            {
                // Limpiar datos relacionados para reinsertar frescos
                await _payment.EliminarDetallesPagoAsync(existingDetail);
              
                detallesPago = existingDetail;
                _logger.LogInformation("Detalle de pago {Id} actualizado en BD", id);
            }

            // Actualizar campos principales
            detallesPago.Intent = detalles.Intent;
            detallesPago.OrderStatus = detalles.Status;
            detallesPago.PayerEmail = detalles.Payer?.Email;
            detallesPago.PayerFirstName = detalles.Payer?.Name?.GivenName;
            detallesPago.PayerLastName = detalles.Payer?.Name?.Surname;
            detallesPago.PayerId = detalles.Payer?.PayerId;

            // Información de envío
            var firstUnit = detalles.PurchaseUnits?.FirstOrDefault();
            if (firstUnit != null)
            {
                var informacionEnvio = new PayPalPaymentShipping
                {
                    PaymentId = detalles.Id,
                    RecipientName = firstUnit.Shipping?.Name?.FullName,
                    AddressLine1 = firstUnit.Shipping?.Address?.AddressLine1,
                    City = firstUnit.Shipping?.Address?.AdminArea2,
                    State = firstUnit.Shipping?.Address?.AdminArea1,
                    PostalCode = firstUnit.Shipping?.Address?.PostalCode,
                    CountryCode = firstUnit.Shipping?.Address?.CountryCode
                };
                await _payment.AgregarInfoEnvioAsync(informacionEnvio);

                // Montos
                detallesPago.AmountTotal = _conversion.ConvertToDecimal(firstUnit.Amount?.Value);
                detallesPago.AmountCurrency = firstUnit.Amount?.CurrencyCode;
                detallesPago.AmountItemTotal = _conversion.ConvertToDecimal(firstUnit.Amount?.Breakdown?.ItemTotal?.Value);

                // Calcular subtotal si es necesario
                if (detallesPago.AmountItemTotal == 0 && firstUnit.Items != null)
                {
                    detallesPago.AmountItemTotal = firstUnit.Items.Sum(item =>
                        _conversion.ConvertToDecimal(item.UnitAmount?.Value.ToString()) *
                        _conversion.ConvertToInt(item.Quantity?.ToString()));
                }

                detallesPago.AmountShipping = _conversion.ConvertToDecimal(firstUnit.Amount?.Breakdown?.Shipping?.Value);
                detallesPago.PayeeMerchantId = firstUnit.Payee?.MerchantId;
                detallesPago.PayeeEmail = firstUnit.Payee?.EmailAddress;
                detallesPago.Description = firstUnit.Description;

                // Tracking
                var firstTracker = firstUnit.Shipping?.Trackers?.FirstOrDefault();
                if (firstTracker != null)
                {
                    detallesPago.TrackingId = firstTracker.Id;
                    detallesPago.TrackingStatus = firstTracker.Status;
                }

                // Captures
                if (firstUnit.Payments?.Captures != null)
                {
                    foreach (var capture in firstUnit.Payments.Captures.Where(c => c != null))
                    {
                        

                        // Solo crear si no existe
                        var paypalCapture = new PayPalPaymentCapture
                        {
                            PaymentId = detallesPago.Id,
                            CaptureId = capture.Id,
                            Status = capture.Status,
                            PedidoId = pedidoId,           
                            Amount = _conversion.ConvertToDecimal(capture.Amount?.Value),
                            Currency = capture.Amount?.CurrencyCode,
                            ProtectionEligibility = capture.SellerProtection?.Status,
                            TransactionFeeAmount = _conversion.ConvertToDecimal(capture.SellerReceivableBreakdown?.PaypalFee?.Value),
                            TransactionFeeCurrency = capture.SellerReceivableBreakdown?.PaypalFee?.CurrencyCode,
                            ReceivableAmount = _conversion.ConvertToDecimal(capture.SellerReceivableBreakdown?.NetAmount?.Value),
                            ReceivableCurrency = capture.SellerReceivableBreakdown?.NetAmount?.CurrencyCode,
                            FinalCapture = capture.FinalCapture,
                            CreateTime = _conversion.ConvertToDateTime(capture.CreateTime),
                            UpdateTime = _conversion.ConvertToDateTime(capture.UpdateTime),
                        };

                        if (decimal.TryParse(capture.SellerReceivableBreakdown?.ExchangeRate?.Value,
                            NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchangeRate))
                            paypalCapture.ExchangeRate = exchangeRate;

                        if (capture.SellerProtection?.DisputeCategories != null)
                            paypalCapture.DisputeCategories = JsonConvert.SerializeObject(capture.SellerProtection.DisputeCategories);

                        await _payment.AgregarCaptureAsync(paypalCapture);
                    }
                }

                // Items
                if (firstUnit.Items != null)
                {
                    foreach (var item in firstUnit.Items)
                    {
                        var paymentItem = new PayPalPaymentItem
                        {
                            PayPalId = detallesPago.Id,
                            ItemName = item.Name,
                            ItemSku = item.Sku,
                            ItemPrice = _conversion.ConvertToDecimal(item.UnitAmount?.Value),
                            ItemCurrency = item.UnitAmount?.CurrencyCode,
                            ItemTax = _conversion.ConvertToDecimal(item.Tax?.Value),
                            ItemQuantity = _conversion.ConvertToInt(item.Quantity)
                        };
                        await _payment.AgregarPagoItemAsync(paymentItem);
                    }
                }
            }

            return OperationResult<PayPalPaymentDetail>.Ok("", detallesPago);
        }


        public async Task<OperationResult<Pedido>> ConfirmarPagoDelPedidoAsync(int usuarioActual, string captureId, decimal total, string? currency, string orderId)
        {
           
                // Validar parámetros de entrada
                if (string.IsNullOrWhiteSpace(captureId)  ||
                string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(orderId))
                {
                    _logger.LogWarning("Parámetros inválidos en ConfirmarPagoDelPedidoAsync: usuarioActual={UsuarioId}, captureId={CaptureId}, total={Total}, currency={Currency}, orderId={OrderId}",
                        usuarioActual, captureId, total, currency, orderId);
                    return OperationResult<Pedido>.Fail("Datos no validos");
                }
                var pedido = await _pedidoRepository.ObtenerPedidoEnProcesoUsuarioAsync(usuarioActual);

                if (pedido == null)
                {
                    _logger.LogWarning("No se encontró un pedido en proceso para el usuario {UsuarioId}", usuarioActual);
                    return OperationResult<Pedido>.Fail("Pedido no encontrado para el usuario especificado");
                }

                pedido.CaptureId = captureId;
                pedido.Total = total;
                pedido.Currency = currency;
                pedido.OrderId = orderId;
                pedido.EstadoPedido = EstadoPedido.Pagado.ToString();
                await _pedidoRepository.ActualizarPedidoAsync(pedido);
                return OperationResult<Pedido>.Ok("", pedido);
            
        }
        public async Task ProcesarRembolsoAsync(int pedidoId, string status, string refundId)
        {

            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(pedidoId);
            
            if (pedido == null)
                throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");

            pedido.EstadoPedido = status;
            pedido.RefundId = refundId;
            foreach (var detalle in pedido.DetallePedidos)
            {
                detalle.Rembolsado = true;
            }
            await _pedidoRepository.ActualizarPedidoAsync(pedido);

            var usuarioActual = _currentUserAccesor.GetCurrentUserId();

            // Crear o actualizar registro de reembolso
            var obtenerRembolso = await _paypalRepository.ObtenRembolsoAsync(pedido.NumeroPedido);

            if (obtenerRembolso == null)
            {
                var rembolso = new Rembolso
                {

                    NumeroPedido = pedido.NumeroPedido,
                    NombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto,
                    EmailCliente = pedido.IdUsuarioNavigation?.Email,
                    FechaRembolso = DateTime.UtcNow,
                    MotivoRembolso = "Rembolso solicitado por el usuario",
                    EstadoRembolso = "REMBOLSO APROVADO",
                    RembosoRealizado = true,
                    UsuarioId = usuarioActual,
                    PedidoId = pedido.Id,
                 
                };
                await _paypalRepository.AgregarRembolsoAsync(rembolso);
            }
            else
            {
                obtenerRembolso.EstadoRembolso = "REMBOLSO APROVADO";
                obtenerRembolso.RembosoRealizado = true;
               
                obtenerRembolso.FechaRembolso = DateTime.UtcNow;
                

                await _paypalRepository.ActualizarRembolsoAsync(obtenerRembolso);
            }


            _logger.LogInformation($"Estado del pedido {pedidoId} actualizado a {status}");


        }

    }
}
