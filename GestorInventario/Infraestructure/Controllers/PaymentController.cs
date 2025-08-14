
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using GestorInventario.Interfaces.Application;
using GestorInventario.MetodosExtension;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.Application.DTOs.Email;

namespace GestorInventario.Infraestructure.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IPaypalService _paypalService;
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly IMemoryCache _memory;
        private readonly IEmailService _emailService;     
        private readonly PolicyExecutor _policyExecutor;
        public PaymentController(ILogger<PaymentController> logger,  IConfiguration configuration, GestorInventarioContext context, IMemoryCache memory, 
            IEmailService email, PolicyExecutor executor, IPaypalService service)
        {
            _logger = logger;          
            _configuration = configuration;
            _context = context;
            _memory = memory;
            _emailService = email;
            _policyExecutor = executor;
            _paypalService = service;
        }
        public async Task<IActionResult> Success()
        {
            try
            {
                if (!_memory.TryGetValue("PayPalOrderId", out string orderId) || string.IsNullOrEmpty(orderId))
                {
                    throw new Exception("No se encontró el ID del pedido en el caché.");
                }

                var (captureId, total, currency) = await _paypalService.CapturarPagoAsync(orderId);

                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(existeUsuario, out int usuarioId))
                {
                    throw new Exception("No se pudo obtener el ID del usuario autenticado.");
                }

                var pedido = await _context.Pedidos
                    .Where(p => p.IdUsuario == usuarioId && p.EstadoPedido == "En Proceso")
                    .OrderByDescending(p => p.FechaPedido)
                    .FirstOrDefaultAsync();

                if (pedido == null)
                {
                    throw new Exception("No se encontró un pedido en proceso para este usuario.");
                }

                pedido.CaptureId = captureId; //-> Localizado en el array captures dentro de la respuesta de PayPal representa el id de la venta
                pedido.Total = total;
                pedido.Currency = currency;
                pedido.OrderId = orderId;
                pedido.EstadoPedido = "Pagado";

                await _context.UpdateEntityAsync(pedido);
         
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al realizar el pago");
                return RedirectToAction("Error", "Home");
            }
        }

        //Si el pago es reembolsado
        [HttpPost]
        public async Task<IActionResult> RefundSale(RefundRequestModel request)
        {
            if (request == null || request.PedidoId <= 0)
            {
                return BadRequest("Solicitud inválida.");
            }

            try
            {
                var refund = await _paypalService.RefundSaleAsync(request.PedidoId, request.currency);

                return RedirectToAction("Index", "Pedidos");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al realizar el reembolso: {ex.Message}");
            }
        }
        public async Task<IActionResult> FormularioRembolso()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> FormularioRembolso(RefundForm form)
        {
            try
            {
                var obtenerNumeroPedido = await _policyExecutor.ExecutePolicyAsync(() => _context.Pedidos
                    .Where(p => p.NumeroPedido == form.NumeroPedido)
                    .FirstOrDefaultAsync());

                if (obtenerNumeroPedido == null)
                {
                    return BadRequest("El numero de pedido proporcionado no existe");
                }

                var existeUsuario = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(existeUsuario, out int usuarioId))
                {
                    return BadRequest("Usuario no válido");
                }

                var emailCliente = await _policyExecutor.ExecutePolicyAsync(() => _context.Usuarios
                    .Where(u => u.Id == usuarioId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync());

                if (emailCliente == null)
                {
                    return BadRequest("El cliente no existe");
                }

                var pedido = await _policyExecutor.ExecutePolicyAsync(() => _context.Pedidos
                    .FirstOrDefaultAsync(p => p.NumeroPedido == form.NumeroPedido));

                if (pedido == null)
                {
                    return BadRequest("Pedido no encontrado");
                }

                var detallespago = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalService.ObtenerDetallesPagoEjecutadoV2(pedido.OrderId));

                if (detallespago == null)
                {
                    return BadRequest("Error en obtener detalles");
                }

                // Verificar que hay purchase units
                if (detallespago.PurchaseUnits == null || !detallespago.PurchaseUnits.Any())
                {
                    return BadRequest("No se encontraron unidades de compra");
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
                    detallesSuscripcion.TransactionsTotal = ConvertToDecimal(firstPurchaseUnit.Amount.Value);
                    detallesSuscripcion.TransactionsCurrency = firstPurchaseUnit.Amount.CurrencyCode;

                    if (firstPurchaseUnit.Amount.Breakdown != null)
                    {
                        detallesSuscripcion.TransactionsSubtotal = ConvertToDecimal(
                            firstPurchaseUnit.Amount.Breakdown.ItemTotal?.Value ?? "0");
                        detallesSuscripcion.TransactionsShipping = ConvertToDecimal(
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
                    detallesSuscripcion.SaleState = firstCapture.Status;

                    if (firstCapture.Amount != null)
                    {
                        detallesSuscripcion.SaleTotal = ConvertToDecimal(firstCapture.Amount.Value);
                        detallesSuscripcion.SaleCurrency = firstCapture.Amount.CurrencyCode;
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
                    UsuarioId = usuarioId,
                    NumeroPedido = form.NumeroPedido,
                    NombreCliente = form.NombreCliente,
                    EmailCliente = emailCliente,
                    FechaRembolso = form.FechaRembolso,
                    EstadoRembolso = "EN REVISION PARA APROBACION",
                    MotivoRembolso = form.MotivoRembolso
                };

                await _context.AddEntityAsync(rembolso);

                // Preparar y enviar el correo a los empleados
                var emailRembolso = new EmailRembolsoDto
                {
                    NumeroPedido = rembolso.NumeroPedido,
                    NombreCliente = rembolso.NombreCliente,
                    EmailCliente = rembolso.EmailCliente,
                    FechaRembolso = rembolso.FechaRembolso,
                    MotivoRembolso = rembolso.MotivoRembolso,
                    Productos = paypalItems
                };

                await _policyExecutor.ExecutePolicy(() => _emailService.SendEmailAsyncRembolso(emailRembolso));

                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                // Loggear el error
                _logger.LogError(ex, "Error al procesar el reembolso");
                return StatusCode(500, "Ocurrió un error al procesar tu solicitud");
            }
        }
        private decimal? ConvertToDecimal(object value)
        {
            if (value == null)
            {
                return null;
            }
            // Si el valor es un decimal, simplemente lo devuelve
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
            return null; // Si no puede convertir el valor, devuelve null
        }
        private int? ConvertToInt(object value)
        {
            if (value == null)
            {
                return null;
            }
            // Si el valor ya es un int, simplemente lo devuelve
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
            return null; // Si no puede convertir el valor, devuelve null
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
                // Convertir el valor a cadena, asumiendo que es un tipo no-string
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