using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.Paypal;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GestorInventario.Infraestructure.Repositories
{
    public class PaymentRepository:IPaymentRepository
    {
        public readonly GestorInventarioContext _context;
        private readonly ILogger<PaypalRepository> _logger;
        private readonly IEmailService _emailService;
        public PaymentRepository(GestorInventarioContext context, ILogger<PaypalRepository> logger, IEmailService email)
        {
            _context = context;
            _logger = logger;
            _emailService = email;
        }
        public async Task<string?> ObtenerEmailUsuarioAsync(int usuarioId)
        {
            var email = await _context.Usuarios
                .Where(u => u.Id == usuarioId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
            return email;
        }
        public async Task<(Pedido?,string)> ObtenerNumeroPedido(RefundForm form)
        {
            var numeroPedido= await _context.Pedidos.FirstOrDefaultAsync(p => p.NumeroPedido == form.NumeroPedido);
            return numeroPedido is null ? (null,"El numero del pedido no se encuentra"):(numeroPedido,"NumeroPedido encontrado");
        }
        public async Task<(Pedido?, string)> AgregarInfoPedido(int usuarioActual, string? captureId, string? total, string? currency, string? orderId)
        {
            // Validar parámetros de entrada
            if (string.IsNullOrWhiteSpace(captureId) || string.IsNullOrWhiteSpace(total) ||
                string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogWarning("Parámetros inválidos en AgregarInfoPedido: usuarioActual={UsuarioId}, captureId={CaptureId}, total={Total}, currency={Currency}, orderId={OrderId}",
                    usuarioActual, captureId, total, currency, orderId);
                return (null, "Los datos proporcionados son inválidos.");
            }

            var pedido = await _context.Pedidos
                .Where(p => p.IdUsuario == usuarioActual && p.EstadoPedido == "En Proceso")
                .OrderByDescending(p => p.FechaPedido)
                .FirstOrDefaultAsync();

            if (pedido == null)
            {
                _logger.LogWarning("No se encontró un pedido en proceso para el usuario {UsuarioId}", usuarioActual);
                return (null, "No se encontró un pedido en proceso para este usuario.");
            }

            try
            {
                pedido.CaptureId = captureId;
                pedido.Total = total;
                pedido.Currency = currency;
                pedido.OrderId = orderId;
                pedido.EstadoPedido = "Pagado";

                await _context.UpdateEntityAsync(pedido);
                return (pedido, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el pedido para usuario {UsuarioId}, captureId={CaptureId}", usuarioActual, captureId);
                return (null, "Ocurrió un error al actualizar el pedido.");
            }
        }
        public PayPalPaymentDetail ProcesarDetallesSuscripcion( CheckoutDetails detallespago)
        {
            if (detallespago.PurchaseUnits == null || !detallespago.PurchaseUnits.Any())
            {
                _logger.LogInformation("No se encuentran las unidades de pago en la peticion");
                throw new InvalidOperationException("No purchase units found");
            }

            var firstPurchaseUnit = detallespago.PurchaseUnits.First();

            var detallesSuscripcion = new PayPalPaymentDetail
            {
                Id = detallespago.Id,
                Intent = detallespago.Intent,
                Status = detallespago.Status,
                PaymentMethod = "paypal",
                PayerEmail = detallespago.Payer?.Email,
                PayerFirstName = detallespago.Payer?.Name?.GivenName,
                PayerLastName = detallespago.Payer?.Name?.Surname,
                PayerId = detallespago.Payer?.PayerId,
                ShippingRecipientName = firstPurchaseUnit?.Shipping?.Name?.FullName,
                ShippingLine1 = firstPurchaseUnit?.Shipping?.Address?.AddressLine1,
                ShippingCity = firstPurchaseUnit?.Shipping?.Address?.AdminArea2,
                ShippingState = firstPurchaseUnit?.Shipping?.Address?.AdminArea1,
                ShippingPostalCode = firstPurchaseUnit?.Shipping?.Address?.PostalCode,
                ShippingCountryCode = firstPurchaseUnit?.Shipping?.Address?.CountryCode,
            };

            // Procesar el amount
            if (firstPurchaseUnit?.Amount != null)
            {
                detallesSuscripcion.AmountTotal = ConvertToDecimal(firstPurchaseUnit.Amount.Value);
                detallesSuscripcion.AmountCurrency = firstPurchaseUnit.Amount.CurrencyCode;

                if (firstPurchaseUnit.Amount.Breakdown != null)
                {
                    detallesSuscripcion.AmountItemTotal = ConvertToDecimal(
                        firstPurchaseUnit.Amount.Breakdown.ItemTotal?.Value ?? "0");
                    detallesSuscripcion.AmountShipping = ConvertToDecimal(
                        firstPurchaseUnit.Amount.Breakdown.Shipping?.Value ?? "0");
                }
            }

            // Procesar payee
            if (firstPurchaseUnit?.Payee != null)
            {
                detallesSuscripcion.PayeeMerchantId = firstPurchaseUnit.Payee.MerchantId;
                detallesSuscripcion.PayeeEmail = firstPurchaseUnit.Payee.EmailAddress;
            }

            detallesSuscripcion.Description = firstPurchaseUnit?.Description;

            // Procesar pagos y capturas
            if (firstPurchaseUnit?.Payments?.Captures != null && firstPurchaseUnit.Payments.Captures.Any())
            {
                var firstCapture = firstPurchaseUnit.Payments.Captures.First();
                detallesSuscripcion.SaleId = firstCapture.Id;
                detallesSuscripcion.CaptureStatus = firstCapture.Status;

                if (firstCapture.Amount != null)
                {
                    detallesSuscripcion.CaptureAmount = ConvertToDecimal(firstCapture.Amount.Value);
                    detallesSuscripcion.CaptureCurrency = firstCapture.Amount.CurrencyCode;
                }

                if (firstCapture.SellerProtection != null)
                {
                    detallesSuscripcion.ProtectionEligibility = firstCapture.SellerProtection.Status;
                }

                // Procesar seller receivable breakdown con verificaciones de nulidad
                if (firstCapture.SellerReceivableBreakdown != null)
                {
                    detallesSuscripcion.TransactionFeeAmount = firstCapture.SellerReceivableBreakdown.PaypalFee != null ?
                        ConvertToDecimal(firstCapture.SellerReceivableBreakdown.PaypalFee.Value) : 0;

                    detallesSuscripcion.TransactionFeeCurrency = firstCapture.SellerReceivableBreakdown.PaypalFee?.CurrencyCode;

                    detallesSuscripcion.ReceivableAmount = firstCapture.SellerReceivableBreakdown.NetAmount != null ?
                        ConvertToDecimal(firstCapture.SellerReceivableBreakdown.NetAmount.Value) : 0;

                    detallesSuscripcion.ReceivableCurrency = firstCapture.SellerReceivableBreakdown.NetAmount?.CurrencyCode;

                    if (firstCapture.SellerReceivableBreakdown.ExchangeRate != null &&
                        !string.IsNullOrEmpty(firstCapture.SellerReceivableBreakdown.ExchangeRate.Value))
                    {
                        if (decimal.TryParse(firstCapture.SellerReceivableBreakdown.ExchangeRate.Value,
                            NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchangeRate))
                        {
                            detallesSuscripcion.ExchangeRate = exchangeRate;
                        }
                    }
                }
            }

            return detallesSuscripcion;
        }
        public async Task<(PayPalPaymentItem?,string)> ProcesarRembolso(PurchaseUnitsBse firstPurchaseUnit, PayPalPaymentDetail detallesSuscripcion,int usuarioActual, RefundForm form,Pedido obtenerNumeroPedido, string emailCliente)
        {
            // Lista para almacenar los ítems de PayPal
            var paypalItems = new List<PayPalPaymentItem>();

            // Procesar items
            if (firstPurchaseUnit?.Items != null)
            {
                foreach (var item in firstPurchaseUnit.Items)
                {
                    var paymentItem = new PayPalPaymentItem
                    {
                        PayPalId = detallesSuscripcion.Id,
                        ItemName = item.Name,
                        ItemSku = item.Sku,
                        ItemPrice = item.UnitAmount != null ? ConvertToDecimal(item.UnitAmount.Value) : 0,
                        ItemCurrency = item.UnitAmount?.CurrencyCode,
                        ItemTax = item.Tax != null ? ConvertToDecimal(item.Tax.Value) : 0,
                        ItemQuantity = ConvertToInt(item.Quantity)
                    };

                    paypalItems.Add(paymentItem);
                }
            }

            // Crear el reembolso
            var rembolso = new Rembolso
            {
                UsuarioId = usuarioActual,
                NumeroPedido = form.NumeroPedido,
                NombreCliente = form.NombreCliente,
                EmailCliente = emailCliente,
                FechaRembolso = form.FechaRembolso,
                EstadoRembolso = "EN REVISION PARA APROBACION",
                MotivoRembolso = form.MotivoRembolso,
                PedidoId = obtenerNumeroPedido.Id,
            };

            await _context.AddEntityAsync(rembolso);

            
            var emailRembolso = new EmailRembolsoDto
            {
                NumeroPedido = rembolso.NumeroPedido,
                NombreCliente = rembolso.NombreCliente,
                EmailCliente = rembolso.EmailCliente,
                FechaRembolso = rembolso.FechaRembolso,
                MotivoRembolso = rembolso.MotivoRembolso,
                Productos = paypalItems
            };
            await  _emailService.EnviarEmailSolicitudRembolso(emailRembolso);

            if (!paypalItems.Any())
            {
                _logger.LogError($"No se encontraron ítems en PurchaseUnits para el reembolso del pedido {form.NumeroPedido}.");
                return (null,"No se puede procesar un reembolso sin ítems asociados.");
            }
            return (paypalItems.First(),"Rembolso procesado con exito");

        }
        public decimal? ConvertToDecimal(object value)
        {
            if (value == null)
            {
                return null;
            }
            
            if (value is decimal decimalValue)
            {
                return decimalValue;
            }
            // Si el valor es una cadena, intenta convertirlo a decimal
            if (value is string stringValue)
            {
                if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            // Si el valor es de otro tipo, intenta convertirlo explícitamente a string y luego a decimal
            try
            {
                var stringRepresentation = value.ToString();
                if (decimal.TryParse(stringRepresentation, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar la conversión");
            }
            return null; 
        }
        public int? ConvertToInt(object value)
        {
            if (value == null)
            {
                return null;
            }
           
            if (value is int intValue)
            {
                return intValue;
            }
            // Si el valor es una cadena, intenta convertirlo a int
            if (value is string stringValue)
            {
                if (int.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            // Si el valor es de otro tipo, intenta convertirlo explícitamente a string y luego a int
            try
            {
                var stringRepresentation = value.ToString();
                if (int.TryParse(stringRepresentation, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar la conversión a int");
            }
            return null; 
        }
        public DateTime? ConvertToDateTime(object value)
        {
            if (value == null)
            {
                return null;
            }
            // Si el valor ya es un DateTime, simplemente lo devuelve
            if (value is DateTime dateTimeValue)
            {
                return dateTimeValue;
            }
            // Convertir el valor a cadena si no es ya un string
            string stringValue;
            if (value is string strValue)
            {
                stringValue = strValue;
            }
            else
            {
                
                stringValue = value.ToString();
            }
            // Quitar corchetes si están presentes
            stringValue = stringValue.Trim('{', '}').Trim();
            // Intentar convertir usando formatos específicos
            var formats = new[]
            {
                "dd/MM/yyyy HH:mm:ss",   // Formato de 24 horas
                "dd/MM/yyyy H:mm:ss",    // Formato de 24 horas sin ceros a la izquierda
                "dd/MM/yyyy h:mm:ss tt", // Formato de 12 horas con AM/PM
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-dd",
                "MM/dd/yyyy",
                "MM-dd-yyyy"
            };
            // Intentar convertir con los formatos definidos
            if (DateTime.TryParseExact(stringValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }
            Console.WriteLine($"No se pudo convertir. Formatos intentados: {string.Join(", ", formats)}");

            return null;
        }
    }
}
