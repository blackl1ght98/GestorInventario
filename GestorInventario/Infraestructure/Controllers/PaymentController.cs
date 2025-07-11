
using Microsoft.AspNetCore.Mvc;

using System.Globalization;
using System.Text;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Caching.Memory;
using GestorInventario.Application.DTOs;
using GestorInventario.Interfaces.Application;
using GestorInventario.MetodosExtension;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Domain.Models.ViewModels.paypal;

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
        public async Task<IActionResult> Success(string PayerID)
        {
            try
            {
                // Recuperar el orderId desde el caché
                if (!_memory.TryGetValue("PayPalPaymentId", out string orderId) || string.IsNullOrEmpty(orderId))
                {
                    throw new Exception("No se encontró el ID del pedido en el caché.");
                }

                using (var httpClient = new HttpClient())
                {
                    var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
                    var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
                    var authToken = await _paypalService.GetAccessTokenAsync(clientId, clientSecret);

                    if (string.IsNullOrEmpty(authToken))
                        throw new Exception("No se pudo obtener el token de autenticación.");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Capturar el pedido con /v2/checkout/orders/{orderId}/capture
                    var response = await httpClient.PostAsync($"https://api-m.sandbox.paypal.com/v2/checkout/orders/{orderId}/capture", new StringContent("{}", Encoding.UTF8, "application/json"));
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Error al ejecutar el pago: {response.StatusCode} - {responseBody}");
                    }

                    // Parsear la respuesta de la API v2
                    var jsonResponse = JObject.Parse(responseBody);
                    var captureId = jsonResponse["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["id"]?.ToString();
                    var total = jsonResponse["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["amount"]?["value"]?.ToString();
                    var currency = jsonResponse["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["amount"]?["currency_code"]?.ToString();

                    if (string.IsNullOrEmpty(captureId) || string.IsNullOrEmpty(total) || string.IsNullOrEmpty(currency))
                    {
                        throw new Exception("No se pudo extraer la información del pago.");
                    }

                    // Actualizar la base de datos manualmente
                    var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(existeUsuario, out int usuarioId))
                    {
                        var pedido = await _context.Pedidos
                            .Where(p => p.IdUsuario == usuarioId && p.EstadoPedido == "En Proceso")
                            .OrderByDescending(p => p.FechaPedido)
                            .FirstOrDefaultAsync();

                        if (pedido != null)
                        {
                            pedido.SaleId = captureId; // En v2, esto es el captureId, no saleId
                            pedido.Total = total;
                            pedido.Currency = currency;
                            pedido.PagoId = orderId; // Usamos orderId, no paymentId
                            pedido.EstadoPedido = "Pagado";
                            _context.Update(pedido);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            throw new Exception("No se encontró un pedido en proceso para este usuario.");
                        }
                    }
                    else
                    {
                        throw new Exception("No se pudo obtener el ID del usuario autenticado.");
                    }

                    return View();
                }
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al realizar el pago");
                return RedirectToAction("Error", "Home");
            }
        }
        //Si el pago es rechazado viene aqui
        [HttpPost]
        public async Task<IActionResult> RefundSale([FromBody] RefundRequestModel request)
        {
            if (request == null || request.PedidoId <= 0)
            {
                return BadRequest("Solicitud inválida.");
            }

            try
            {
                var refund = await _paypalService.RefundSaleAsync(request.PedidoId, request.currency);
                return Ok(refund);
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
            var obtenerNumeroPedido = await _policyExecutor.ExecutePolicyAsync(()=> _context.Pedidos
                .Where(p => p.NumeroPedido == form.NumeroPedido)
                .FirstOrDefaultAsync()) ;
            if (obtenerNumeroPedido == null)
            {
                return BadRequest("El numero de pedido proporcionado no existe");
            }
            var existeUsuario = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int usuarioId;
            var rembolso = new Rembolso();
            if (int.TryParse(existeUsuario, out usuarioId))
            {
                var emailCliente = await _policyExecutor.ExecutePolicyAsync(() => _context.Usuarios
                    .Where(u => u.Id == usuarioId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync());
                if (emailCliente == null)
                {
                    return BadRequest("El cliente no existe");
                }
                var pedido = await _policyExecutor.ExecutePolicyAsync(() => _context.Pedidos.FirstOrDefaultAsync(p => p.NumeroPedido == form.NumeroPedido));
                var existingDetail = await _policyExecutor.ExecutePolicyAsync(()=> _context.PayPalPaymentDetails
                    .Include(d => d.PayPalPaymentItems)
                    .FirstOrDefaultAsync(x => x.Id == pedido.PagoId)) ;
                var detallespago = await _policyExecutor.ExecutePolicyAsync(()=> _paypalService.ObtenerDetallesPagoEjecutadoV2(pedido.PagoId)) ;
                if (detallespago == null)
                {
                    return BadRequest("Error en obtener detalles");
                }

                var detallesSuscripcion = new PayPalPaymentDetail
                {
                    Id = detallespago["id"]?.ToString() ?? string.Empty,
                    Intent = detallespago["intent"]?.ToString() ?? string.Empty,
                    Status = detallespago["status"]?.ToString() ?? string.Empty,
                    PaymentMethod = "paypal",
                    PayerEmail = detallespago["payer"]?["email_address"]?.ToString() ?? string.Empty,
                    PayerFirstName = detallespago["payer"]?["name"]?["given_name"]?.ToString() ?? string.Empty,
                    PayerLastName = detallespago["payer"]?["name"]?["surname"]?.ToString() ?? string.Empty,
                    PayerId = detallespago["payer"]?["payer_id"]?.ToString() ?? string.Empty,
                    ShippingRecipientName = detallespago["purchase_units"]?[0]?["shipping"]?["name"]?["full_name"]?.ToString() ?? string.Empty,
                    ShippingLine1 = detallespago["purchase_units"]?[0]?["shipping"]?["address"]?["address_line_1"]?.ToString() ?? string.Empty,
                    ShippingCity = detallespago["purchase_units"]?[0]?["shipping"]?["address"]?["admin_area_2"]?.ToString() ?? string.Empty,
                    ShippingState = detallespago["purchase_units"]?[0]?["shipping"]?["address"]?["admin_area_1"]?.ToString() ?? string.Empty,
                    ShippingPostalCode = detallespago["purchase_units"]?[0]?["shipping"]?["address"]?["postal_code"]?.ToString() ?? string.Empty,
                    ShippingCountryCode = detallespago["purchase_units"]?[0]?["shipping"]?["address"]?["country_code"]?.ToString() ?? string.Empty
                };



                // Lista para almacenar los ítems de PayPal temporalmente
                var paypalItems = new List<PayPalPaymentItem>();

                if (detallespago["purchase_units"] != null)
                {
                    foreach (var purchaseUnit in detallespago["purchase_units"])
                    {
                        if (purchaseUnit != null)
                        {
                            detallesSuscripcion.TransactionsTotal = ConvertToDecimal(purchaseUnit?["amount"]?["value"]?.ToString());
                            detallesSuscripcion.TransactionsCurrency = purchaseUnit?["amount"]?["currency_code"]?.ToString() ?? string.Empty;
                            detallesSuscripcion.TransactionsSubtotal = ConvertToDecimal(purchaseUnit?["amount"]?["breakdown"]?["item_total"]?["value"]?.ToString());
                            if (detallesSuscripcion.TransactionsSubtotal == 0 && purchaseUnit["items"] != null)
                            {
                                decimal subtotal = 0;
                                foreach (var item in purchaseUnit["items"])
                                {
                                    var unitAmount = ConvertToDecimal(item["unit_amount"]?["value"]?.ToString());
                                    var quantity = ConvertToInt(item["quantity"]?.ToString());
                                    subtotal += unitAmount * quantity;
                                }
                                detallesSuscripcion.TransactionsSubtotal = subtotal;
                            }
                            detallesSuscripcion.TransactionsShipping = ConvertToDecimal(purchaseUnit?["amount"]?["breakdown"]?["shipping"]?["value"]?.ToString());
                            detallesSuscripcion.PayeeMerchantId = purchaseUnit?["payee"]?["merchant_id"]?.ToString() ?? string.Empty;
                            detallesSuscripcion.PayeeEmail = purchaseUnit?["payee"]?["email_address"]?.ToString() ?? string.Empty;
                            detallesSuscripcion.Description = purchaseUnit?["description"]?.ToString() ?? string.Empty;

                            if (purchaseUnit["payments"]?["captures"] != null)
                            {
                                foreach (var capture in purchaseUnit["payments"]["captures"])
                                {
                                    if (capture != null)
                                    {
                                        detallesSuscripcion.SaleId = capture["id"]?.ToString() ?? string.Empty;
                                        detallesSuscripcion.SaleState = capture["status"]?.ToString() ?? string.Empty;
                                        detallesSuscripcion.SaleTotal = ConvertToDecimal(capture["amount"]?["value"]?.ToString());
                                        detallesSuscripcion.SaleCurrency = capture["amount"]?["currency_code"]?.ToString() ?? string.Empty;
                                        detallesSuscripcion.ProtectionEligibility = capture["seller_protection"]?["status"]?.ToString() ?? string.Empty;
                                        detallesSuscripcion.TransactionFeeAmount = ConvertToDecimal(capture["seller_receivable_breakdown"]?["paypal_fee"]?["value"]?.ToString());
                                        detallesSuscripcion.TransactionFeeCurrency = capture["seller_receivable_breakdown"]?["paypal_fee"]?["currency_code"]?.ToString() ?? string.Empty;
                                        detallesSuscripcion.ReceivableAmount = ConvertToDecimal(capture["seller_receivable_breakdown"]?["net_amount"]?["value"]?.ToString());
                                        detallesSuscripcion.ReceivableCurrency = capture["seller_receivable_breakdown"]?["net_amount"]?["currency_code"]?.ToString() ?? string.Empty;
                                        string exchangeRateValue = capture["seller_receivable_breakdown"]?["exchange_rate"]?["value"]?.ToString() ?? string.Empty;
                                        if (!string.IsNullOrEmpty(exchangeRateValue) && decimal.TryParse(exchangeRateValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchangeRate))
                                        {
                                            detallesSuscripcion.ExchangeRate = exchangeRate;
                                        }
                                        detallesSuscripcion.CreateTime = ConvertToDateTime(capture["create_time"]);
                                        detallesSuscripcion.UpdateTime = ConvertToDateTime(capture["update_time"]);
                                    }
                                }
                            }

                            var items = purchaseUnit["items"];
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    var paymentItem = new PayPalPaymentItem
                                    {
                                        PayPalId = detallesSuscripcion.Id,
                                        ItemName = item["name"]?.ToString(),
                                        ItemSku = item["sku"]?.ToString(),
                                        ItemPrice = ConvertToDecimal(item["unit_amount"]?["value"]?.ToString()),
                                        ItemCurrency = item["unit_amount"]?["currency_code"]?.ToString(),
                                        ItemTax = ConvertToDecimal(item["tax"]?["value"]?.ToString()),
                                        ItemQuantity = ConvertToInt(item["quantity"]?.ToString())
                                    };

                                    paypalItems.Add(paymentItem); // Agregar a la lista temporal
                                }
                            }
                        }
                    }

                }

                if (pedido != null && existingDetail != null)
                {
                    rembolso = new Rembolso
                    {
                        UsuarioId = usuarioId,
                        NumeroPedido = form.NumeroPedido,
                        NombreCliente = form.NombreCliente,
                        EmailCliente = emailCliente,
                        FechaRembolso = form.FechaRembolso,
                        EstadoRembolso = "EN REVISION PARA APROBACION",
                        MotivoRembolso = form.MotivoRembolso
                     };

                    // Guardar el reembolso en la base de datos
                  await  _context.AddEntityAsync(rembolso);
                   

                    // Preparar y enviar el correo a los empleados
                    var emailRembolso = new EmailRembolsoDto
                    {
                        NumeroPedido = rembolso.NumeroPedido,
                        NombreCliente = rembolso.NombreCliente,
                        EmailCliente = rembolso.EmailCliente,
                        FechaRembolso = rembolso.FechaRembolso,

                        MotivoRembolso = rembolso.MotivoRembolso,
                        Productos = paypalItems // Usar la lista de ítems recolectada
                    };

                    await _policyExecutor.ExecutePolicy(()=> _emailService.SendEmailAsyncRembolso(emailRembolso)) ;
                }
            }

            return RedirectToAction("Index", "Admin");
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