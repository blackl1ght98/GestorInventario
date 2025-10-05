using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.Response.PayPal;
using GestorInventario.Application.DTOs.Response_paypal;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Application.DTOs.Response_paypal.PATCH;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Application.Exceptions;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace GestorInventario.Application.Services
{
    public class PaypalService : IPaypalService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaypalService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IUnitOfWork _unitOfWork;     
        private readonly IEmailService _emailService;
        private readonly IHttpClientFactory _httpClientFactory;     
        private readonly UtilityClass _utilityClass;
        private readonly GestorInventarioContext _context;
        public PaypalService(IConfiguration configuration, ILogger<PaypalService> logger, IHttpClientFactory http, UtilityClass utility, GestorInventarioContext context,
            IMemoryCache memory, IUnitOfWork unit,  IEmailService email)
        {

            _configuration = configuration;         
            _logger = logger;
            _cache = memory;
            _unitOfWork = unit;
            _emailService = email;
            _httpClientFactory = http;
           
            _utilityClass = utility;
            _context = context;
        }
        #region Generacion token paypal

        public async Task<string> GetAccessTokenAsync(string clientId, string clientSecret)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var tokenUrl = "v1/oauth2/token";
            var byteArray = Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
            var authHeader = Convert.ToBase64String(byteArray);

            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            });

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonConvert.DeserializeObject<TokenResponsePayPal>(responseString);
            if (responseJson?.AccessToken == null)
            {
                throw new InvalidOperationException("No se pudo obtener el token de acceso.");
            }

            return responseJson.AccessToken;
        }
        private (string clientId, string clientSecret) GetPaypalCredentials()
        {
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            if (clientId == null || clientSecret == null)
            {
                throw new InvalidOperationException("No se puede obtener el cliente id o secreto de cliente");
            }
            return (clientId, clientSecret);
        }

        #endregion

        #region Crear orden 
        public async Task<string> CreateOrderWithPaypalAsync(Checkout pagar)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PayPal");
                var order = BuildOrder(pagar);
                var (clientId, clientSecret) = GetPaypalCredentials();

                var authToken = await GetAccessTokenAsync(clientId, clientSecret);
                if (string.IsNullOrEmpty(authToken))
                    throw new Exception("No se pudo obtener el token de autenticación.");

                var request = new HttpRequestMessage(HttpMethod.Post, "v2/checkout/orders");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                request.Content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error al crear la orden: {response.StatusCode} - {responseBody}");
                }

                var jsonResponse = JsonConvert.DeserializeObject<PayPalOrderResponse>(responseBody);
               
                var orderId = jsonResponse?.Id;

                if (!string.IsNullOrEmpty(orderId))
                {
                    _cache.Set("PayPalOrderId", orderId, TimeSpan.FromMinutes(10));
                }

                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la orden de PayPal");
                throw new InvalidOperationException("No se pudo crear la orden de PayPal", ex);
            }
        }

        private PaypalCreateOrderRequest BuildOrder(Checkout pagar)
        {
            return new PaypalCreateOrderRequest
            {
                Intent = "CAPTURE",
                PurchaseUnits = new List<PurchaseUnit>
            {
                new PurchaseUnit
                {
                    Amount = new AmountBase
                    {
                        CurrencyCode = pagar.Currency,
                        Value = pagar.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),
                        Breakdown = new Breakdown
                        {
                            ItemTotal = new MoneyOrder
                            {
                                CurrencyCode = pagar.Currency,
                                Value = pagar.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)
                            },
                            TaxTotal = new MoneyOrder
                            {
                                CurrencyCode = pagar.Currency,
                                Value = "0.00"
                            },
                            ShippingAmount = new MoneyOrder
                            {
                                CurrencyCode = pagar.Currency,
                                Value = "0.00"
                            }
                        }
                    },
                    Description = "The payment transaction description.",
                    InvoiceId = Guid.NewGuid().ToString(),
                    Items = pagar.Items.ConvertAll(item => new Item
                    {
                        Name = item.Name,
                        Description = item.Description,
                        Quantity = item.Quantity.ToString(),
                        UnitAmount = new MoneyOrder
                        {
                            Value = item.Price.ToString("F2", CultureInfo.InvariantCulture),
                            CurrencyCode = pagar.Currency
                        },
                        Tax = new MoneyOrder
                        {
                            Value = "0.00",
                            CurrencyCode = pagar.Currency
                        },
                        Sku = item.Sku
                    }),
                    Shipping = new Shipping
                    {
                        Name = new NameClientOrder
                        {
                            FullName = pagar.NombreCompleto
                        },
                        Address = new Address
                        {
                            AddressLine1 = pagar.Line1,
                            AddressLine2 = pagar.Line2 ?? "",
                            City = pagar.Ciudad,
                            State = "ES",
                            PostalCode = pagar.CodigoPostal,
                            CountryCode = "ES"
                        }
                    }
                }
            },
                    PaymentSource = new PaymentSource
                    {
                        Paypal = new Paypal
                        {
                            ExperienceContext = new ExperienceContext
                            {
                                PaymentMethodPreference = "IMMEDIATE_PAYMENT_REQUIRED",
                                ReturnUrl = pagar.ReturnUrl,
                                CancelUrl = pagar.CancelUrl
                            }
                        }
                    }
                };
        }
        #endregion

        #region Capturar el pago (orden)
        public async Task<(string CaptureId, string Total, string Currency)> CapturarPagoAsync(string orderId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PayPal");
                var (clientId, clientSecret) = GetPaypalCredentials();

                var authToken = await GetAccessTokenAsync(clientId, clientSecret);
                if (string.IsNullOrEmpty(authToken))
                    throw new Exception("No se pudo obtener el token de autenticación.");

                var request = new HttpRequestMessage(HttpMethod.Post, $"v2/checkout/orders/{orderId}/capture");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json"); 

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error al capturar el pago: {response.StatusCode} - {responseBody}");
                }

                var paypalResponse = JsonConvert.DeserializeObject<PaypalCaptureOrderResponse>(responseBody);

                var capture = paypalResponse?.PurchaseUnits?
                    .FirstOrDefault()?.Payments?
                    .Captures?.FirstOrDefault();

                if (capture == null || string.IsNullOrEmpty(capture.Id) ||
                    string.IsNullOrEmpty(capture.Amount?.Value) ||
                    string.IsNullOrEmpty(capture.Amount?.CurrencyCode))
                {
                    throw new Exception("No se pudo extraer la información de la captura del pago.");
                }

                return (capture.Id, capture.Amount.Value, capture.Amount.CurrencyCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al capturar el pago de PayPal");
                throw new InvalidOperationException("No se pudo capturar el pago de PayPal", ex);
            }
        }

        #endregion

        #region Obtener detalles del pago v2 paypal   
        public async Task<CheckoutDetails> ObtenerDetallesPagoEjecutadoV2(string id)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var (clientId, clientSecret) = GetPaypalCredentials();

            var authToken = await GetAccessTokenAsync(clientId, clientSecret);
            if (string.IsNullOrEmpty(authToken))
                throw new Exception("No se pudo obtener el token de autenticación.");

            // Creamos el mensaje de la solicitud GET
            var request = new HttpRequestMessage(HttpMethod.Get, $"v2/checkout/orders/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Enviamos la solicitud
            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error al obtener los detalles del pago: {response.StatusCode} - {responseBody}");
            }

            // Deserializamos la respuesta al DTO correspondiente
            var subscriptionDetails = JsonConvert.DeserializeObject<CheckoutDetails>(responseBody);
            if(subscriptionDetails == null)
            {
                throw new ArgumentNullException("No se puede obtener los detalles de la subscripcion");
            }
            return subscriptionDetails;
           
        }
        #endregion

        #region Seguimiento pedido
        public async Task<string> SeguimientoPedido(int pedidoId, Carrier carrier, BarcodeType barcode)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PayPal");
                var (pedido, detalles) = await _unitOfWork.PaypalRepository.GetPedidoConDetallesAsync(pedidoId);
                if (pedido == null || detalles == null)
                {
                    throw new Exception("No se pudo obtener la información completa del pedido.");
                }              
                var trackingInfo = CrearTrackingInfo(pedido, detalles, carrier, barcode);             
                var (clientId, clientSecret) = GetPaypalCredentials();
                var authToken = await GetAccessTokenAsync(clientId, clientSecret);
                if (string.IsNullOrEmpty(authToken))
                {
                    throw new Exception("No se pudo obtener el token de autenticación de PayPal.");
                }
                var request = new HttpRequestMessage(HttpMethod.Post,$"v2/checkout/orders/{pedido.OrderId}/track");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                request.Content = new StringContent(JsonConvert.SerializeObject(trackingInfo), Encoding.UTF8, "application/json");
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error al agregar seguimiento. Status: {response.StatusCode}, Response: {responseBody}");
                    throw new Exception($"Error al agregar el seguimiento: {response.StatusCode}");
                }
                // Actualizar el estado del pedido usando UnitOfWork
                await _unitOfWork.PaypalRepository.AddInfoTrackingOrder(pedidoId,trackingInfo.TrackingNumber,"", carrier.ToString());
                _logger.LogInformation("Seguimiento agregado exitosamente para el pedido {PedidoId}", pedidoId);
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar seguimiento para el pedido {PedidoId}", pedidoId);
                throw new InvalidOperationException($"No se pudo agregar el seguimiento para el pedido {pedidoId}.", ex);
            }
        }
        private PayPalTrackingInfo CrearTrackingInfo(Pedido pedido, IEnumerable<DetallePedido> detalles, Carrier carrier, BarcodeType barcode)
        {
            var trackingItems = detalles.Select(item => new TrackingItems
            {
                Name = item.Producto?.NombreProducto ?? "Producto no disponible",
                Sku = item.Producto?.Descripcion ?? "N/A",
                Quantity = item.Cantidad ?? 1,
                Upc = new Upc
                {
                    Type = barcode,
                    Code = item.Producto?.UpcCode ?? "N/A"
                },
                ImageUrl = item.Producto?.Imagen ?? string.Empty,
                Url = string.Empty
            }).ToList();

            return new PayPalTrackingInfo
            {
                CaptureId = pedido.CaptureId,
                TrackingNumber = GenerarNumeroSeguimiento(),
                Carrier = carrier,
                NotifyPayer = true,
                Items = trackingItems
            };
        }
        private  string GenerarNumeroSeguimiento()
        {
            // Prefijo opcional para identificar el tipo de pedido
            string prefijo = "PKG";

            // Fecha y hora para hacerlo único
            string fecha = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            // Parte aleatoria para reducir riesgo de colisión
            string aleatorio = new Random().Next(1000, 9999).ToString();

            // Concatenamos todo
            return $"{prefijo}-{fecha}-{aleatorio}";
        }
        #endregion

        #region Realizar reembolso 
        public async Task<string> RefundSaleAsync(int pedidoId, string currency)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PayPal");
                // Obtener el pedido y el monto total desde el repositorio
                var (pedido, totalAmount) = await _unitOfWork.PaypalRepository.GetPedidoWithDetailsAsync(pedidoId);
               
                // Crear el objeto de solicitud de reembolso
                var refundRequest = BuildRefundRequest(totalAmount, pedido);

                var (clientId, clientSecret) = GetPaypalCredentials();
                var authToken = await GetAccessTokenAsync(clientId, clientSecret);
                if (string.IsNullOrEmpty(authToken))
                    throw new Exception("No se pudo obtener el token de autenticación.");

                var request = new HttpRequestMessage(HttpMethod.Post, $"v2/payments/captures/{pedido.CaptureId}/refund");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                request.Content = new StringContent(JsonConvert.SerializeObject(refundRequest), Encoding.UTF8, "application/json");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
               
                // Enviamos la solicitud
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error al obtener los detalles del pago: {response.StatusCode} - {responseBody}");
                }

                _logger.LogInformation("Reembolso exitoso: {response}", responseBody);
               
                var refundResponse = JsonConvert.DeserializeObject<PaypalRefundResponse>(responseBody);
                if(refundResponse == null)
                {
                    throw new  ArgumentNullException("No se pudo obtener los destalles de la devolucion");
                }
                string refundId = refundResponse.Id;
                var updatedCapture = await ObtenerDetallesPagoEjecutadoV2(pedido.OrderId);
                if (updatedCapture == null)
                {
                    throw new ArgumentNullException("No se pudo obtener los detalles actualizados");
                }
                string estadoVenta = updatedCapture.PurchaseUnits[0].Payments.Captures[0].Status;
                await _unitOfWork.PaypalRepository.UpdatePedidoStatusAsync(pedidoId, "Reembolsado", refundId,estadoVenta);
                await _unitOfWork.PaypalRepository.EnviarEmailNotificacionRembolso(pedidoId, totalAmount, "Rembolso Aprobado");
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el reembolso");
                throw new InvalidOperationException("No se pudo realizar el reembolso", ex);
            }
        }
        private PaypalRefundResponse BuildRefundRequest(decimal totalAmount, Pedido pedido)
        {
            return new PaypalRefundResponse
            {
                NotaParaElCliente = "Pedido rembolsado",
                Amount = new AmountRefund
                {
                    Value = totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                    CurrencyCode = pedido.Currency
                }
            };
        }
        public async Task<string> RefundPartialAsync(int pedidoId, string currency, string motivo)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PayPal");
                var (pedido, totalAmount) = await _unitOfWork.PaypalRepository.GetProductoDePedidoAsync(pedidoId);
                var detalle = pedido.Pedido.DetallePedidos.FirstOrDefault();
                if (detalle == null)
                    throw new ArgumentException($"No se encontró el detalle del pedido con ID {pedidoId}.");

                if (detalle.Rembolsado ?? false)
                    throw new InvalidOperationException("El detalle del pedido ya ha sido reembolsado.");

                // Obtener detalles de la captura
                var captureDetails = await ObtenerDetallesPagoEjecutadoV2(pedido.Pedido.OrderId);
                if (captureDetails == null)
                    throw new InvalidOperationException("No se pudieron obtener los detalles de la captura.");

                // Obtener el net_amount y calcular el monto disponible
                var capture = captureDetails.PurchaseUnits[0].Payments.Captures[0];
                var netAmount = decimal.Parse(capture.SellerReceivableBreakdown.NetAmount.Value, CultureInfo.InvariantCulture);

                // Calcular el monto total reembolsado desde los reembolsos previos
                var refundedAmount = captureDetails.PurchaseUnits[0].Payments.Refunds?.Sum(r => decimal.Parse(r.SellerPayableBreakdown.NetAmount.Value, CultureInfo.InvariantCulture)) ?? 0m;
                var availableAmount = netAmount - refundedAmount;

                _logger.LogInformation("Monto neto: {NetAmount} AUD, Monto reembolsado previamente: {RefundedAmount} AUD, Monto disponible: {AvailableAmount} AUD, Monto solicitado: {TotalAmount} AUD",
                    netAmount, refundedAmount, availableAmount, totalAmount);

                if (totalAmount > availableAmount)
                {
                    _logger.LogWarning(
                        "El monto solicitado para el reembolso ({TotalAmount} {Currency}) excede el monto disponible ({AvailableAmount} AUD) para el pedido {PedidoId}. Ajustando a {AvailableAmount} AUD.",
                        totalAmount, currency, availableAmount, pedidoId, availableAmount);
                    totalAmount = availableAmount;
                }

                if (availableAmount <= 0)
                {
                    _logger.LogError("No hay monto disponible para reembolsar para el pedido {PedidoId}.", pedidoId);
                    throw new InvalidOperationException("No hay monto disponible para reembolsar.");
                }

                // Verificar que la moneda coincida
                var captureCurrency = capture.Amount.CurrencyCode;
                if (currency != captureCurrency)
                {
                    _logger.LogWarning("La moneda solicitada ({Currency}) no coincide con la moneda de la captura ({CaptureCurrency})", currency, captureCurrency);
                    throw new InvalidOperationException($"La moneda del reembolso ({currency}) no coincide con la moneda de la captura ({captureCurrency}).");
                }

                var refundRequest = BuildRefundPartialRequest(totalAmount, pedido);
                var requestJson = JsonConvert.SerializeObject(refundRequest);
                _logger.LogInformation("Refund request JSON: {RequestJson}", requestJson);

                var (clientId, clientSecret) = GetPaypalCredentials();
                var authToken = await GetAccessTokenAsync(clientId, clientSecret);
                if (string.IsNullOrEmpty(authToken))
                    throw new Exception("No se pudo obtener el token de autenticación.");

                var request = new HttpRequestMessage(HttpMethod.Post, $"v2/payments/captures/{pedido.Pedido.CaptureId}/refund");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                // Consultar el estado de la captura después del reembolso (o en caso de error para verificar falsos positivos)
                var updatedCapture = await ObtenerDetallesPagoEjecutadoV2(pedido.Pedido.OrderId);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = JsonConvert.DeserializeObject<PaypalErrorResponse>(responseBody);
                    string errorMessage = $"Error al procesar el reembolso: {response.StatusCode} - {errorDetails?.Message ?? "Error desconocido"}";
                    if (errorDetails?.Details?.Any(d => d.Issue == "REFUND_AMOUNT_EXCEEDED") == true)
                    {
                        errorMessage = $"El monto del reembolso ({totalAmount} {currency}) excede el monto disponible ({availableAmount} AUD).";
                        // Verificar si el reembolso ya se procesó
                        var recentRefund = updatedCapture?.PurchaseUnits[0].Payments.Refunds?
                            .FirstOrDefault(r => r.Amount.Value == totalAmount.ToString("F2", CultureInfo.InvariantCulture));
                        if (recentRefund != null)
                        {
                            _logger.LogWarning("Falso positivo detectado: Reembolso de {TotalAmount} AUD ya procesado con ID {RefundId}.", totalAmount, recentRefund.Id);
                            responseBody = JsonConvert.SerializeObject(new PaypalRefundResponse { Id = recentRefund.Id });
                        }
                        else
                        {
                            _logger.LogError("Error en la solicitud de reembolso a PayPal: {errorMessage}. Response: {responseBody}", errorMessage, responseBody);
                            throw new InvalidOperationException(errorMessage);
                        }
                    }
                    else if (errorDetails?.Details?.Any(d => d.Issue != null) == true)
                    {
                        errorMessage += $". Detalles: {string.Join(", ", errorDetails.Details.Select(d => $"{d.Issue}: {d.Description}"))}";
                        _logger.LogError("Error en la solicitud de reembolso a PayPal: {errorMessage}. Response: {responseBody}", errorMessage, responseBody);
                        throw new InvalidOperationException(errorMessage);
                    }
                }

                _logger.LogInformation("Reembolso exitoso: {response}", responseBody);
                var refundResponse = JsonConvert.DeserializeObject<PaypalRefundResponse>(responseBody);
                string refundId = refundResponse.Id;
                var producto = pedido.Producto.NombreProducto;
                string cadena = $"El producto reembolsado es {producto}";

                // Truncar la cadena para evitar errores de longitud en la base de datos
                const int maxLength = 30;
                if (cadena.Length > maxLength)
                {
                    cadena = cadena.Substring(0, maxLength - 3) + "...";
                    _logger.LogWarning("EstadoRembolso truncado a {MaxLength} caracteres: {Cadena}", maxLength, cadena);
                }

                // Usar updatedCapture para obtener el estado de la captura
                string estadoVenta = updatedCapture?.PurchaseUnits[0].Payments.Captures[0].Status ?? "PARTIALLY_REFUNDED";

                await _unitOfWork.PaypalRepository.RegistrarReembolsoParcialAsync(
                    pedido.Pedido.Id,
                    detalle.Id,
                    cadena,
                    refundId,
                    totalAmount,
                    motivo,
                    estadoVenta
                );

                // Enviar notificación por correo
                var (emailSuccess, emailMessage) = await _unitOfWork.PaypalRepository.EnviarEmailNotificacionRembolso(
                    pedido.Pedido.Id,
                    totalAmount,
                    motivo
                );
                if (!emailSuccess)
                {
                    _logger.LogWarning("No se pudo enviar el correo de notificación: {EmailMessage}", emailMessage);
                }

                return responseBody;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validación fallida al realizar el reembolso para pedidoId {pedidoId}", pedidoId);
                throw new InvalidOperationException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al realizar el reembolso para pedidoId {pedidoId}", pedidoId);
                throw new InvalidOperationException("No se pudo realizar el reembolso. Por favor, intenta de nuevo o contacta al soporte.", ex);
            }
        }
        private PaypalRefundResponse BuildRefundPartialRequest(decimal totalAmount, DetallePedido pedido)
        {
            return new PaypalRefundResponse
            {
                NotaParaElCliente = "Pedido rembolsado",
                Amount = new AmountRefund
                {
                    Value = totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                    CurrencyCode = pedido.Pedido.Currency,
                }
            };
        }

        #endregion

        #region creacion de un producto y plan de suscripcion
        public async Task<CreateProductResponse> CreateProductAsync(string productName, string productDescription, string productType, string productCategory)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var (clientId, clientSecret) = GetPaypalCredentials();
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            var productRequest = BuildProductRequest(productName, productDescription, productType, productCategory);

            var request = new HttpRequestMessage(HttpMethod.Post, $"v1/catalogs/products/");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            request.Content = new StringContent(JsonConvert.SerializeObject(productRequest), Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);

            // Check if the request was successful
            response.EnsureSuccessStatusCode(); 

            var responseBody = await response.Content.ReadAsStringAsync();
            var productResponse = JsonConvert.DeserializeObject<CreateProductResponse>(responseBody);
            if (productResponse == null) 
            {
                throw new ArgumentNullException("No se pudo obtener el producto");
            }
            return productResponse;
        }

        // Update the BuildProductRequest to use the request DTO
        private CreateProductRequest BuildProductRequest(string productName, string productDescription, string productType, string productCategory)
        {
            return new CreateProductRequest
            {
                Nombre = productName,
                Description = productDescription,
                Type = productType,
                Category = productCategory,
                Imagen = "https://example.com/product-image.jpg"
            };
        }

        public async Task<string> CreateSubscriptionPlanAsync(string productId, string planName, string description, decimal amount, string currency, int trialDays = 0, decimal trialAmount = 0.00m)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            var client = _httpClientFactory.CreateClient("PayPal");
            var (clientId, clientSecret) = GetPaypalCredentials();
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            try
            {
                var planRequest = BuildPaypalPlanRequest(productId, planName, description, amount, currency, trialDays, trialAmount);

                var request = new HttpRequestMessage(HttpMethod.Post, "v1/billing/plans");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                request.Content = new StringContent(JsonConvert.SerializeObject(planRequest), Encoding.UTF8, "application/json");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var responseObject = JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(responseBody);
                    string createdPlanId = responseObject.Id;

                    // Guardar los detalles del plan en la base de datos con la ID de PayPal
                    await _unitOfWork.PaypalRepository.SavePlanDetailsAsync(createdPlanId, planRequest);

                    return responseBody;
                }
                else
                {
                    throw new Exception($"Error al crear el plan de suscripción: {response.StatusCode} - {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el plan de suscripción");
                return $"{{\"error\":\"Se produjo un error al crear el plan de suscripción: {ex.Message}\"}}";
            }
        }
        private PaypalPlanDetailsDto BuildPaypalPlanRequest(string productId, string planName, string description, decimal amount, string currency, int trialDays = 0, decimal trialAmount = 0.00m)
        {
            var billingCycles = new List<BillingCycleDto>();

            // Si hay días de prueba, agrega el ciclo de prueba
            if (trialDays > 0)
            {
                billingCycles.Add(new BillingCycleDto
                {
                    TenureType = "TRIAL",
                    Sequence = 1,
                    Frequency = new FrequencyDto
                    {
                        IntervalUnit = "DAY",
                        IntervalCount = trialDays
                    },
                    TotalCycles = 1,
                    PricingScheme = new PricingSchemeDto
                    {
                        FixedPrice = new FixedPriceDto
                        {
                            Value = trialAmount.ToString("0.00", CultureInfo.InvariantCulture),
                            CurrencyCode = currency
                        }
                    }
                });
            }

            // Agrega el ciclo regular
            billingCycles.Add(new BillingCycleDto
            {
                TenureType = "REGULAR",
                Sequence = trialDays > 0 ? 2 : 1,
                Frequency = new FrequencyDto
                {
                    IntervalUnit = "MONTH",
                    IntervalCount = 1
                },
                TotalCycles = 12,
                PricingScheme = new PricingSchemeDto
                {
                    FixedPrice = new FixedPriceDto
                    {
                        Value = amount.ToString("0.00", CultureInfo.InvariantCulture),
                        CurrencyCode = currency
                    }
                }
            });

            return new PaypalPlanDetailsDto
            {
                ProductId = productId,
                Name = planName,
                Description = description,
                Status = "ACTIVE",
                BillingCycles = billingCycles.ToArray(),
                PaymentPreferences = new PaymentPreferencesDto
                {
                    AutoBillOutstanding = true,
                    SetupFee = new FixedPriceDto
                    {
                        Value = "0.00",
                        CurrencyCode = currency
                    },
                    SetupFeeFailureAction = "CONTINUE",
                    PaymentFailureThreshold = 3
                },
                Taxes = new TaxesDto
                {
                    Percentage = "10",
                    Inclusive = false
                }
            };
        }
        #endregion

        #region Obtener detalles del plan 
        public async Task<PaypalPlanResponse> ObtenerDetallesPlan(string id)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogError("El ID del plan no puede ser nulo o vacío.");
                throw new ArgumentException("El ID del plan es requerido.");
            }
            try
            {
                var (clientId, clientSecret) = GetPaypalCredentials();
                var authToken = await GetAccessTokenAsync(clientId, clientSecret);
                if (string.IsNullOrEmpty(authToken))
                    throw new Exception("No se pudo obtener el token de autenticación.");

                var request = new HttpRequestMessage(HttpMethod.Get, $"v1/billing/plans/{id}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);             
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Enviamos la solicitud
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error al obtener los detalles del plan {id}: {response.StatusCode}, {responseBody}");
                    throw new Exception($"Error al obtener los detalles del plan: {response.StatusCode}, {responseBody}");
                }

                // Deserializa la respuesta a PaypalPlanResponse
                try
                {
                    var planDetails = JsonConvert.DeserializeObject<PaypalPlanResponse>(responseBody);
                    if (planDetails == null)
                    {
                        _logger.LogError($"No se pudo deserializar la respuesta del plan {id}: {responseBody}");
                        throw new Exception("La respuesta de PayPal no contiene datos válidos.");
                    }

                    _logger.LogInformation($"Detalles del plan {id} obtenidos correctamente.");
                    return planDetails;
                }
                catch (JsonException ex)
                {
                    _logger.LogError($"Error al deserializar la respuesta del plan {id}: {ex.Message}, Contenido: {responseBody}");
                    throw new Exception("Error al deserializar los detalles del plan.", ex);
                }

            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Error de red al obtener los detalles del plan {id}: {ex.Message}");
                throw new Exception("Error de red al comunicarse con PayPal.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error inesperado al obtener los detalles del plan {id}: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Obtener producto asociado a un plan
        public async Task<(PaypalProductListResponse ProductsResponse, bool HasNextPage)> GetProductsAsync(int page = 1, int pageSize = 10)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var (clientId, clientSecret) = GetPaypalCredentials();
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);
            if (string.IsNullOrEmpty(authToken))
                throw new Exception("No se pudo obtener el token de autenticación.");


            var request = new HttpRequestMessage(HttpMethod.Get, $"v1/catalogs/products?page_size={pageSize}&page={page}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Enviamos la solicitud
            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<PaypalProductListResponse>(responseContent);
                if (jsonResponse == null)
                {
                    throw new ArgumentNullException("No se ha podido obtener los productos");

                }
                bool hasNextPage = jsonResponse.Links.Any(link => link.Rel == "next");

                return (jsonResponse, hasNextPage);

            }
            else
            {

                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al obtener productos: {response.StatusCode} - {errorContent}");
            }

        }
        #endregion

        #region Obtener Planes de suscripcion
        public async Task<(List<PaypalPlanResponse> plans, bool HasNextPage)> GetSubscriptionPlansAsyncV2(int page = 1, int pageSize = 6)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var (clientId, clientSecret) = GetPaypalCredentials();
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);
            if (string.IsNullOrEmpty(authToken))
                throw new Exception("No se pudo obtener el token de autenticación.");


            var request = new HttpRequestMessage(HttpMethod.Get, $"v1/billing/plans?page_size={pageSize}&page={page}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Enviamos la solicitud
            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al obtener planes: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Respuesta JSON de /v1/billing/plans: {Content}", responseContent);

            // Deserializamos usando el DTO tipado para la lista
            var plansListResponse = JsonConvert.DeserializeObject<PaypalPlansListResponse>(responseContent);
            if (plansListResponse == null)
            {
                throw new ArgumentNullException("No se pudo obtener los planes");
            }
            bool hasNextPage = plansListResponse.Links.Any(link => link.Rel == "next");

            var detailedPlans = new List<PaypalPlanResponse>();

            // Obtener detalles completos de cada plan (si quieres detalles completos)
            foreach (var plan in plansListResponse.Plans)
            {
                try
                {
                    var requestPlanDetails = new HttpRequestMessage(HttpMethod.Get, $"v1/billing/plans/{plan.Id}");
                    requestPlanDetails.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                    requestPlanDetails.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Enviamos la solicitud
                    var planResponse = await client.SendAsync(requestPlanDetails);

                    if (!planResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await planResponse.Content.ReadAsStringAsync();
                        _logger.LogWarning($"Error al obtener detalles del plan {plan.Id}: {planResponse.StatusCode} - {errorContent}");
                        continue; // Omitir y continuar con el siguiente plan
                    }
                    var planDetailsContent = await planResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("Detalles JSON para plan {PlanId}: {Content}", plan.Id, planDetailsContent);

                    var planDetails = JsonConvert.DeserializeObject<PaypalPlanResponse>(planDetailsContent);

                    if (planDetails != null)
                        detailedPlans.Add(planDetails);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al procesar detalles del plan {plan.Id}: {ex.Message}");

                }
            }

            return (detailedPlans, hasNextPage);

        }
        #endregion

        #region Editar producto vinculado a un plan
        public async Task<string> EditarProducto(string id, string name, string description)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            // Validar parámetros de entrada
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("El ID del producto no puede ser nulo o vacío.", nameof(id));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("El nombre del producto no puede ser nulo o vacío.", nameof(name));
            }
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentException("La descripción del producto no puede ser nula o vacía.", nameof(description));
            }

            var (clientId, clientSecret) = GetPaypalCredentials();
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);
            if (string.IsNullOrEmpty(authToken))
                throw new Exception("No se pudo obtener el token de autenticación.");       
            try
            {

                var productrequest = new HttpRequestMessage(HttpMethod.Get, $"v1/catalogs/products/{id}");
                productrequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
               
                productrequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Enviamos la solicitud
                var productResponse = await client.SendAsync(productrequest);
                var responseBody = await productResponse.Content.ReadAsStringAsync();
                if (!productResponse.IsSuccessStatusCode)
                {
                    var errorContent = await productResponse.Content.ReadAsStringAsync();
                    throw new Exception($"No se pudo encontrar el producto con ID {id}: {productResponse.StatusCode} - {errorContent}");
                }
                // Crear la solicitud PATCH usando DTOs
                var patchRequest = BuildEditProductRequest(name, description);

                var pathrequest = new HttpRequestMessage(HttpMethod.Patch, $"v1/catalogs/products/{id}");
                pathrequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                pathrequest.Content = new StringContent(JsonConvert.SerializeObject(patchRequest), Encoding.UTF8, "application/json");
                pathrequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Enviamos la solicitud
                var response = await client.SendAsync(pathrequest);
                var patchResponse = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al actualizar el producto con ID {id}: {response.StatusCode} - {errorContent}");
                }

                return "Producto actualizado con éxito";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el producto con ID {ProductId}", id);
                throw new  Exception("Ocurrio un error al actualizar el producto"); 
            }
        }

        private List<PatchOperation> BuildEditProductRequest(string name, string description)
        {
            return new List<PatchOperation>
                {
                    new PatchOperation
                    {
                        Operation = "replace",
                        Path = "/name",
                        Value = name
                    },
                    new PatchOperation
                    {
                        Operation = "replace",
                        Path = "/description",
                        Value = description
                    }
                };
        }
        #endregion

       
        #region Actualizar precios de un plan
        public async Task<string> UpdatePricingPlanAsync(string planId, decimal? trialAmount, decimal regularAmount, string currency)
        {
            SetInvariantCulture();
            var (clientId, clientSecret) = GetPaypalCredentials();
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);
            if (string.IsNullOrEmpty(authToken))
                throw new Exception("No se pudo obtener el token de autenticación.");

            try
            {
                var planDetails = await GetPlanDetailsAsync(planId, authToken);
                var pricingUpdates = DeterminePricingUpdates(planDetails, trialAmount, regularAmount, currency, planId);

                if (!pricingUpdates.Any())
                {
                    throw new PayPalException("No se proporcionaron esquemas de precios válidos para actualizar el plan. Los precios enviados son idénticos a los actuales o no se proporcionaron cambios.");
                }

                await UpdatePlanPricingAsync(planId, authToken, pricingUpdates);
                await VerifyAndSavePlanUpdateAsync(planId, authToken, pricingUpdates);

                return "Precio del plan actualizado con éxito";
            }
            catch (PayPalException ex)
            {
                _logger.LogError(ex, "Error al actualizar el precio del plan");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al actualizar el precio del plan");
                throw new PayPalException($"Error inesperado al actualizar el precio del plan: {ex.Message}");
            }
        }

        #region Métodos auxiliares privados:  Actualizar precios de un plan
        private void SetInvariantCulture()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }


        private async Task<PaypalPlanDetailsDto> GetPlanDetailsAsync(string planId, string accessToken)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var planDetailsRequest = new HttpRequestMessage(HttpMethod.Get, $"v1/billing/plans/{planId}");
            planDetailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            planDetailsRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(planDetailsRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonConvert.DeserializeObject<PaypalErrorResponse>(responseBody);
                throw new PayPalException($"No se pudo obtener los detalles del plan con ID {planId}: {response.StatusCode} - {errorResponse?.Message ?? "Error desconocido"} (Debug ID: {errorResponse?.DebugId})");
            }

            return JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(responseBody);
        }

        private List<UpdatePricingSchemes> DeterminePricingUpdates(PaypalPlanDetailsDto planDetails, decimal? trialAmount, decimal regularAmount, string currency, string planId)
        {
            var pricingSchemes = new List<UpdatePricingSchemes>();
            var (hasTrial, currentTrialPrice, currentRegularPrice) = AnalyzeCurrentPricing(planDetails);

            HandleTrialPricingUpdate(pricingSchemes, hasTrial, trialAmount, currentTrialPrice, currency, planId);
            HandleRegularPricingUpdate(pricingSchemes, hasTrial, regularAmount, currentRegularPrice, currency, planId);

            return pricingSchemes;
        }

        private (bool hasTrial, decimal? trialPrice, decimal? regularPrice) AnalyzeCurrentPricing(PaypalPlanDetailsDto planDetails)
        {
            bool hasTrial = planDetails.BillingCycles.Any(bc => bc.TenureType == "TRIAL");
            decimal? currentTrialPrice = null;
            decimal? currentRegularPrice = null;

            foreach (var cycle in planDetails.BillingCycles)
            {
                if (cycle.TenureType == "TRIAL" && cycle.PricingScheme?.FixedPrice?.Value != null)
                {
                    currentTrialPrice = decimal.Parse(cycle.PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture);
                }
                else if (cycle.TenureType == "REGULAR" && cycle.PricingScheme?.FixedPrice?.Value != null)
                {
                    currentRegularPrice = decimal.Parse(cycle.PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture);
                }
            }

            return (hasTrial, currentTrialPrice, currentRegularPrice);
        }

        private void HandleTrialPricingUpdate(List<UpdatePricingSchemes> pricingSchemes, bool hasTrial, decimal? trialAmount, decimal? currentTrialPrice, string currency, string planId)
        {
            if (!hasTrial) return;

            if (trialAmount.HasValue && trialAmount != currentTrialPrice)
            {
                pricingSchemes.Add(CreatePricingScheme(1, trialAmount.Value, currency));
                _logger.LogInformation($"Incluyendo ciclo de prueba para el plan {planId} con precio {trialAmount}.");
            }
            else if (trialAmount.HasValue && trialAmount == currentTrialPrice)
            {
                _logger.LogInformation($"El precio de prueba {trialAmount} es idéntico al actual ({currentTrialPrice}) para el plan {planId}. Se omite el ciclo de prueba.");
            }
            else if (!trialAmount.HasValue)
            {
                _logger.LogInformation($"No se proporcionó un precio de prueba para el plan {planId}. Se omite el ciclo de prueba.");
            }
        }

        private void HandleRegularPricingUpdate(List<UpdatePricingSchemes> pricingSchemes, bool hasTrial, decimal regularAmount, decimal? currentRegularPrice, string currency, string planId)
        {
            if (regularAmount != currentRegularPrice)
            {
                pricingSchemes.Add(CreatePricingScheme(hasTrial ? 2 : 1, regularAmount, currency));
                _logger.LogInformation($"Incluyendo ciclo regular para el plan {planId} con precio {regularAmount}.");
            }
            else
            {
                _logger.LogInformation($"El precio regular {regularAmount} es idéntico al actual ({currentRegularPrice}) para el plan {planId}. Se omite el ciclo regular.");
            }
        }

        private UpdatePricingSchemes CreatePricingScheme(int sequence, decimal amount, string currency)
        {
            return new UpdatePricingSchemes
            {
                BillingCycleSequence = sequence,
                PricingScheme = new UpdatePricingScheme
                {
                    FixedPrice = new UpdateFixedPrice
                    {
                        Value = amount.ToString("0.00", CultureInfo.InvariantCulture),
                        CurrencyCode = currency
                    }
                }
            };
        }
        private async Task UpdatePlanPricingAsync(string planId, string accessToken, List<UpdatePricingSchemes> pricingSchemes)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var planRequest = new UpdatePricingPlan { PricingSchemes = pricingSchemes };

            var updatePricingRequest = new HttpRequestMessage(HttpMethod.Post, $"v1/billing/plans/{planId}/update-pricing-schemes");
            updatePricingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            updatePricingRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            updatePricingRequest.Content = new StringContent(JsonConvert.SerializeObject(planRequest), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(updatePricingRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonConvert.DeserializeObject<PaypalErrorResponse>(responseBody);
                HandlePricingUpdateError(errorResponse, planId, response.StatusCode);
            }

            if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new PayPalException($"Respuesta inesperada al actualizar el precio del plan con ID {planId}: {response.StatusCode}");
            }
        }

        private void HandlePricingUpdateError(PaypalErrorResponse errorResponse, string planId, HttpStatusCode statusCode)
        {
            if (errorResponse?.Name == "PRICING_SCHEME_INVALID_AMOUNT")
            {
                throw new PayPalException("El precio proporcionado para uno de los ciclos de facturación es idéntico al precio actual. Por favor, proporciona un precio diferente.");
            }
            if (errorResponse?.Name == "PRICING_SCHEME_UPDATE_NOT_ALLOWED")
            {
                throw new PayPalException("No se puede actualizar el precio de un plan activo con suscripciones asociadas. Por favor, crea un nuevo plan o actualiza las suscripciones individualmente.");
            }

            throw new PayPalException($"No se pudo actualizar el precio del plan con ID {planId}: {statusCode} - {errorResponse?.Message ?? "Error desconocido"} (Debug ID: {errorResponse?.DebugId})");
        }

        private async Task VerifyAndSavePlanUpdateAsync(string planId, string accessToken, List<UpdatePricingSchemes> pricingSchemes)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var verifyPlanDetailsRequest = new HttpRequestMessage(HttpMethod.Get, $"v1/billing/plans/{planId}");
            verifyPlanDetailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            verifyPlanDetailsRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(verifyPlanDetailsRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonConvert.DeserializeObject<PaypalErrorResponse>(responseBody);
                throw new PayPalException($"No se pudo obtener los detalles actualizados del plan con ID {planId}: {response.StatusCode} - {errorResponse?.Message ?? "Error desconocido"} (Debug ID: {errorResponse?.DebugId})");
            }

            var updatedPlanDetails = JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(responseBody);
            await _unitOfWork.PaypalRepository.SavePlanPriceUpdateAsync(planId, new UpdatePricingPlan { PricingSchemes = pricingSchemes });
        }

        #endregion
        #endregion

        #region Suscribirse a un plan
        public async Task<string> Subscribirse(string id, string returnUrl, string cancelUrl, string planName)
        {

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            var client = _httpClientFactory.CreateClient("PayPal");
            var (clientId, clientSecret) = GetPaypalCredentials();
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            var subscriptionRequest = BuildSubscriptionRequest(id, returnUrl, cancelUrl, planName);

            var request = new HttpRequestMessage(HttpMethod.Post, "v1/billing/subscriptions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            request.Content = new StringContent(JsonConvert.SerializeObject(subscriptionRequest), Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();                   
            if (response.IsSuccessStatusCode)
            {
                var subscriptionJson = JsonConvert.DeserializeObject<SubscriptionCreateRequest>(responseBody);
                if(subscriptionJson == null)
                {
                    throw new ArgumentNullException("No se pudo iniciar el proceso para suscribirse");
                }
                var approvalLink = subscriptionJson?.Links?.FirstOrDefault(link => link.Rel == "approve")?.Href;
                if (string.IsNullOrEmpty(approvalLink))
                {
                    throw new Exception("No se encontró el enlace de aprobación en la respuesta de PayPal.");
                }
                return approvalLink;
            }

            throw new Exception("Ha ocurrido un error");

        }
        private SubscriptionCreateRequest BuildSubscriptionRequest(string id, string returnUrl, string cancelUrl, string planName)
        {
            return new SubscriptionCreateRequest
            {
                PlanId = id,
                ApplicationContext = new ApplicationContext
                {
                    BrandName = planName,
                    Locale = "es-ES",
                    ShippingPreference = "NO_SHIPPING",
                    UserAction = "SUBSCRIBE_NOW",
                    PaymentMethod = new PaymentMethod
                    {
                        PayerSelected = "PAYPAL",
                        PayeePreferred = "IMMEDIATE_PAYMENT_REQUIRED"
                    },
                    ReturnUrl = returnUrl,
                    CancelUrl = cancelUrl
                }
            };
        }
        #endregion

        #region Obtener detalles de la suscripción
        public async Task<PaypalSubscriptionResponse> ObtenerDetallesSuscripcion(string subscription_id)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var (clientId, clientSecret) = GetPaypalCredentials();
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            var request = new HttpRequestMessage(HttpMethod.Get, $"v1/billing/subscriptions/{subscription_id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            request.Content = new StringContent(JsonConvert.SerializeObject("{}"), Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var subscriptionDetails = JsonConvert.DeserializeObject<PaypalSubscriptionResponse>(responseBody);
                if (subscriptionDetails == null)
                {
                    throw new InvalidOperationException("No se pudieron obtener los detalles de la suscripción: la respuesta del servidor no es válida.");
                }
                return subscriptionDetails;
            }
            else
            {
                throw new Exception($"Error al obtener los detalles de la suscripción: {responseBody}");
            }

        }
        #endregion

        #region desactivar plan de suscripcion
        public async Task<string> DesactivarPlan(string planId)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var (payPalClientId, payPalClientSecret) = GetPaypalCredentials();
            var accessToken = await GetAccessTokenAsync(payPalClientId, payPalClientSecret);

            var deactivatePlanRequest = new HttpRequestMessage(HttpMethod.Post, $"v1/billing/plans/{planId}/deactivate");
            deactivatePlanRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            deactivatePlanRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var deactivatePlanResponse = await client.SendAsync(deactivatePlanRequest);
            var deactivatePlanResponseBody = await deactivatePlanResponse.Content.ReadAsStringAsync();

            if (deactivatePlanResponse.IsSuccessStatusCode)
            {
                var planDetailsRequest = new HttpRequestMessage(HttpMethod.Get, $"v1/billing/plans/{planId}");
                planDetailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                planDetailsRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var planDetailsResponse = await client.SendAsync(planDetailsRequest);
                var planDetailsResponseBody = await planDetailsResponse.Content.ReadAsStringAsync();

                if (planDetailsResponse.IsSuccessStatusCode)
                {
                    var planDetails = JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(planDetailsResponseBody);
                    if(planDetails == null)
                    {
                        throw new ArgumentNullException("No se puede desactivar el plan: respuesta del servidor no valida");
                    }
                    string planStatusResult = planDetails.Status;
                    await _unitOfWork.PaypalRepository.UpdatePlanStatusInDatabase(planId, planStatusResult);
                }
                else
                {
                    throw new Exception($"No se pudo obtener los detalles del plan con ID {planId}: {planDetailsResponse.StatusCode} - {planDetailsResponseBody}");
                }
            }
            else
            {
                throw new Exception($"No se pudo desactivar el plan con ID {planId}: {deactivatePlanResponse.StatusCode} - {deactivatePlanResponseBody}");
            }

            return "Plan desactivado con éxito";
        }
        #endregion

        #region Activar plan
        public async Task<string> ActivarPlan(string planId)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var (payPalClientId, payPalClientSecret) = GetPaypalCredentials();
            var accessToken = await GetAccessTokenAsync(payPalClientId, payPalClientSecret);

            var activatePlanRequest = new HttpRequestMessage(HttpMethod.Post, $"v1/billing/plans/{planId}/activate");
            activatePlanRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            activatePlanRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var activatePlanResponse = await client.SendAsync(activatePlanRequest);
            var activatePlanResponseBody = await activatePlanResponse.Content.ReadAsStringAsync();

            if (activatePlanResponse.IsSuccessStatusCode)
            {
                var planDetailsRequest = new HttpRequestMessage(HttpMethod.Get, $"v1/billing/plans/{planId}");
                planDetailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                planDetailsRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var planDetailsResponse = await client.SendAsync(planDetailsRequest);
                var planDetailsResponseBody = await planDetailsResponse.Content.ReadAsStringAsync();

                if (planDetailsResponse.IsSuccessStatusCode)
                {
                    var planDetails = JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(planDetailsResponseBody);
                    if (planDetails == null) 
                    {
                        throw new ArgumentNullException("No se puede activar el plan: respuesta del servidor no valida");
                    }
                    string planStatusResult = planDetails.Status;
                    await _unitOfWork.PaypalRepository.UpdatePlanStatusInDatabase(planId, planStatusResult);
                }
                else
                {
                    throw new Exception($"No se pudo obtener los detalles del plan con ID {planId}: {planDetailsResponse.StatusCode} - {planDetailsResponseBody}");
                }
            }
            else
            {
                throw new Exception($"No se pudo activar el plan con ID {planId}: {activatePlanResponse.StatusCode} - {activatePlanResponseBody}");
            }

            return "Plan activado con éxito";
        }



        #endregion

        #region Cancelar Subscripcion
        public async Task<string> CancelarSuscripcion(string subscription_id, string reason)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var (payPalClientId, payPalClientSecret) = GetPaypalCredentials();
            var accessToken = await GetAccessTokenAsync(payPalClientId, payPalClientSecret);
            var subscriptionRequest =  new
            {
                reason = reason,
            };
            var request = new HttpRequestMessage(HttpMethod.Post, $"v1/billing/subscriptions/{subscription_id}/cancel");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonConvert.SerializeObject(subscriptionRequest), Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();


            var responseContent = await response.Content.ReadAsStringAsync();

            // Verifica si la solicitud fue exitosa
            if (response.IsSuccessStatusCode)
            {
                await ObtenerDetallesSuscripcion(subscription_id);
                return "Suscripción cancelada exitosamente";
            }

            throw new Exception($"Error al cancelar la suscripción: {responseContent}");

        }
        #endregion

        #region Suspender suscripcion
        public async Task<string> SuspenderSuscripcion(string subscription_id, string reason)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var (payPalClientId, payPalClientSecret) = GetPaypalCredentials();
            var accessToken = await GetAccessTokenAsync(payPalClientId, payPalClientSecret);

            var suspendSubscriptionRequest = new HttpRequestMessage(HttpMethod.Post, $"v1/billing/subscriptions/{subscription_id}/suspend");
            suspendSubscriptionRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            suspendSubscriptionRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            suspendSubscriptionRequest.Content = new StringContent(JsonConvert.SerializeObject(new { reason = reason }), Encoding.UTF8,"application/json");

          var suspendSubscriptionResponse = await client.SendAsync(suspendSubscriptionRequest);
          

            if (suspendSubscriptionResponse.IsSuccessStatusCode)
            {
                var subscriptionDetailsRequest = new HttpRequestMessage(HttpMethod.Get, $"v1/billing/subscriptions/{subscription_id}");
                subscriptionDetailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                subscriptionDetailsRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var subscriptionDetailsResponse = await client.SendAsync(subscriptionDetailsRequest);
                var planDetailsResponseBody = await subscriptionDetailsResponse.Content.ReadAsStringAsync();

                if (subscriptionDetailsResponse.IsSuccessStatusCode)
                {
                    var planDetails = JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(planDetailsResponseBody);
                    if (planDetails == null) 
                    {
                        throw new ArgumentNullException("No se puede suspender la suscripcion: respuesta del servidor no valida");
                    }
                    string planStatusResult = planDetails.Status;
              
                }
                else
                {
                    throw new Exception($"No se pudo obtener los detalles de la suscripcion con ID {subscription_id}: {subscriptionDetailsResponse.StatusCode} - {planDetailsResponseBody}");
                }
            }
            else
            {
                throw new Exception($"No se pudo suspender la suscripcion con ID {subscription_id}: {suspendSubscriptionResponse.StatusCode} - {suspendSubscriptionResponse}");
            }

            return "Subscripcion suspendida con éxito";
        }
        #endregion

        #region Activar suscripcion
        public async Task<string> ActivarSuscripcion(string subscription_id, string reason)
        {
            var client = _httpClientFactory.CreateClient("PayPal");
            var (payPalClientId, payPalClientSecret) = GetPaypalCredentials();
            var accessToken = await GetAccessTokenAsync(payPalClientId, payPalClientSecret);

            var activateSubscriptionRequest = new HttpRequestMessage(HttpMethod.Post, $"v1/billing/subscriptions/{subscription_id}/activate");
            activateSubscriptionRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            activateSubscriptionRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            activateSubscriptionRequest.Content = new StringContent(JsonConvert.SerializeObject(new { reason = reason }), Encoding.UTF8, "application/json");

            var suspendSubscriptionResponse = await client.SendAsync(activateSubscriptionRequest);


            if (suspendSubscriptionResponse.IsSuccessStatusCode)
            {
                var subscriptionDetailsRequest = new HttpRequestMessage(HttpMethod.Get, $"v1/billing/subscriptions/{subscription_id}");
                subscriptionDetailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                subscriptionDetailsRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var subscriptionDetailsResponse = await client.SendAsync(subscriptionDetailsRequest);
                var planDetailsResponseBody = await subscriptionDetailsResponse.Content.ReadAsStringAsync();

                if (subscriptionDetailsResponse.IsSuccessStatusCode)
                {
                    var planDetails = JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(planDetailsResponseBody);
                    if(planDetails == null)
                    {
                        throw new ArgumentNullException("No se puede activar la suscripcion: respuesta del servidor no valida");
                    }
                    string planStatusResult = planDetails.Status;

                }
                else
                {
                    _logger.LogError($"No se pudo obtener los detalles de la suscripcion con ID {subscription_id}: {subscriptionDetailsResponse.StatusCode} - {planDetailsResponseBody}");
                }
            }
            else
            {
                _logger.LogError($"No se pudo activar la subscripcion con ID {subscription_id}: {suspendSubscriptionResponse.StatusCode} - {suspendSubscriptionResponse}");
                
            }

            return "Subscripcion activada con éxito";
        }
        #endregion

        public async Task<CreateProductResponse> CreateProductAndNotifyAsync(string productName, string productDescription, string productType, string productCategory)
        {
            // Crear el producto
            var product = await CreateProductAsync(productName, productDescription, productType, productCategory);
            var usuarioActual =_utilityClass.ObtenerUsuarioIdActual();
            var email = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioActual);

            // Enviar el correo electrónico
            var emailDto = new EmailDto
            {
                ToEmail = email.Email,
                NombreProducto = productName
            };
            await _emailService.SendEmailCreateProduct(emailDto, productName);
            return product;
        }


    }
}