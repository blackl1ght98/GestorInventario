using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Interfaces.Utils;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.order;
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
        public PedidoManagementService( ILogger<PedidoManagementService> logger, IPedidoRepository pedido, ICurrentUserAccessor current,
            IConversionUtils conversion, IPaypalOrderService paypal)
        {
            
            _logger = logger;
            _pedidoRepository = pedido;
            _currentUserAccesor = current;
            _conversion = conversion;
            _paypalOrder = paypal;
        }
        public async Task<OperationResult<string>> EliminarPedido(int Id)
        {
          
                var pedido = await _pedidoRepository.ObtenerPedidoConRembolso(Id);
                if (pedido == null)
                {
                    return OperationResult<string>.Fail("No hay pedido para eliminar");
                }
                if (pedido.EstadoPedido != EstadoPedido.Entregado.ToString())
                {
                    return OperationResult<string>.Fail("El pedido tiene que tener el estado Entregado para ser eliminado y no tener historial asociado");
                }
              
                 await  _pedidoRepository.EliminarPedidoAsync(pedido);
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
        public async Task<OperationResult<PayPalPaymentDetail>> ObtenerDetallePagoEjecutadoV2(string id)
        {

            // Buscar el detalle de pago existente en la base de datos
            var existingDetail = await _pedidoRepository.ObtenerDetallesPago(id);

                // Obtener los detalles actualizados desde la API de PayPal
                var detalles = await _paypalOrder.ObtenerDetallesPagoEjecutadoV2(id);
                if (detalles == null)
                {
                    return OperationResult<PayPalPaymentDetail>.Fail("Detalles del pedido no encontrados para generar la factura");
                }

                // Si el detalle no existe, crear uno nuevo; si existe, actualizarlo
                PayPalPaymentDetail detallesPago;
                if (existingDetail == null)
                {
                    detallesPago = new PayPalPaymentDetail
                    {
                        Id = detalles.Id
                    };
                  await  _pedidoRepository.AgregarDetallePagoAsync(detallesPago);
                }
                else
                {
                    detallesPago = existingDetail;
                // Opcional: Limpiar los ítems existentes si se desea actualizarlos completamente
                await _pedidoRepository.EliminarDetallesPagoAsync(detallesPago);
            }

                // Actualizar los campos del objeto PayPalPaymentDetail con los datos de la API
                detallesPago.Intent = detalles.Intent;
                detallesPago.Status = detalles.Status;
                detallesPago.PaymentMethod = "paypal";
                detallesPago.PayerEmail = detalles.Payer?.Email;
                detallesPago.PayerFirstName = detalles.Payer?.Name?.GivenName;
                detallesPago.PayerLastName = detalles.Payer?.Name?.Surname;
                detallesPago.PayerId = detalles.Payer?.PayerId;

                detallesPago.ShippingRecipientName = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Name?.FullName;
                detallesPago.ShippingLine1 = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.AddressLine1;
                detallesPago.ShippingCity = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.AdminArea2;
                detallesPago.ShippingState = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.AdminArea1;
                detallesPago.ShippingPostalCode = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.PostalCode;
                detallesPago.ShippingCountryCode = detalles.PurchaseUnits?.FirstOrDefault()?.Shipping?.Address?.CountryCode;

                if (detalles.PurchaseUnits != null)
                {
                    foreach (var purchaseUnit in detalles.PurchaseUnits)
                    {
                        if (purchaseUnit != null)
                        {
                            detallesPago.AmountTotal = _conversion.ConvertToDecimal(purchaseUnit.Amount?.Value);
                            detallesPago.AmountCurrency = purchaseUnit.Amount?.CurrencyCode;
                            detallesPago.AmountItemTotal = _conversion.ConvertToDecimal(purchaseUnit.Amount?.Breakdown?.ItemTotal?.Value);

                            // Calcular subtotal si es necesario
                            if (detallesPago.AmountItemTotal == 0 && purchaseUnit.Items != null)
                            {
                                decimal? subtotal = 0;
                                foreach (var item in purchaseUnit.Items)
                                {

                                    var unitAmount = _conversion.ConvertToDecimal(item.UnitAmount?.Value.ToString());
                                    var quantity = _conversion.ConvertToInt(item.Quantity?.ToString());
                                    subtotal += unitAmount * quantity;
                                }
                                detallesPago.AmountItemTotal = subtotal;
                            }

                            detallesPago.AmountShipping = _conversion.ConvertToDecimal(purchaseUnit.Amount?.Breakdown?.Shipping?.Value);
                            detallesPago.PayeeMerchantId = purchaseUnit.Payee?.MerchantId;
                            detallesPago.PayeeEmail = purchaseUnit.Payee?.EmailAddress;
                            detallesPago.Description = purchaseUnit.Description;

                            if (purchaseUnit.Payments?.Captures != null)
                            {
                                foreach (var capture in purchaseUnit.Payments.Captures)
                                {
                                    if (capture != null)
                                    {

                                        detallesPago.SaleId = capture.Id;
                                        detallesPago.CaptureStatus = capture.Status;
                                        detallesPago.CaptureAmount = _conversion.ConvertToDecimal(capture.Amount?.Value);
                                        detallesPago.CaptureCurrency = capture.Amount?.CurrencyCode;
                                        detallesPago.ProtectionEligibility = capture.SellerProtection?.Status;
                                        detallesPago.TransactionFeeAmount = _conversion.ConvertToDecimal(capture.SellerReceivableBreakdown?.PaypalFee?.Value);
                                        detallesPago.TransactionFeeCurrency = capture.SellerReceivableBreakdown?.PaypalFee?.CurrencyCode;
                                        detallesPago.ReceivableAmount = _conversion.ConvertToDecimal(capture.SellerReceivableBreakdown?.NetAmount?.Value);
                                        detallesPago.ReceivableCurrency = capture.SellerReceivableBreakdown?.NetAmount?.CurrencyCode;

                                        var exchangeRateValue = capture.SellerReceivableBreakdown?.ExchangeRate?.Value;
                                        if (decimal.TryParse((string)exchangeRateValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchangeRate))
                                        {
                                            detallesPago.ExchangeRate = exchangeRate;
                                        }
                                        detallesPago.CreateTime = _conversion.ConvertToDateTime(capture.CreateTime);
                                        detallesPago.UpdateTime = _conversion.ConvertToDateTime(capture.UpdateTime);

                                    }

                                }
                                var firstPurchaseUnit = detalles.PurchaseUnits?.FirstOrDefault();
                                if (firstPurchaseUnit != null)
                                {
                                    // Campos de tracking
                                    var firstTracker = firstPurchaseUnit.Shipping?.Trackers?.FirstOrDefault();
                                    if (firstTracker != null)
                                    {
                                        detallesPago.TrackingId = firstTracker.Id;
                                        detallesPago.TrackingStatus = firstTracker.Status;



                                    }

                                    // Campos de captura
                                    var firstCapture = firstPurchaseUnit.Payments?.Captures?.FirstOrDefault();
                                    if (firstCapture != null)
                                    {
                                        detallesPago.FinalCapture = firstCapture.FinalCapture;

                                        if (firstCapture.SellerProtection != null)
                                        {
                                            detallesPago.DisputeCategories =
                                                JsonConvert.SerializeObject(firstCapture.SellerProtection.DisputeCategories);
                                        }
                                    }
                                }

                            }

                            var items = purchaseUnit.Items;
                            if (items != null)
                            {
                                foreach (var item in items)
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
                                  await _pedidoRepository.AgregarPagoItemAsync(paymentItem);
                                }
                            }
                        }
                    }
                }
               

                return OperationResult<PayPalPaymentDetail>.Ok("", detallesPago);
         

        }
        public string GenerarNumeroPedido()
        {
            var length = 10;
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
