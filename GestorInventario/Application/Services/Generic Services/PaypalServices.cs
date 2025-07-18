
using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.Response.PayPal;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Application.DTOs.Response_paypal.PATCH;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace GestorInventario.Application.Services
{
    public class PaypalServices : IPaypalService
    {


        private readonly IConfiguration _configuration;

        private readonly ILogger<PaypalServices> _logger;
        private readonly IMemoryCache _cache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _contextAccesor;
        private readonly IEmailService _emailService;
        public PaypalServices(IConfiguration configuration, ILogger<PaypalServices> logger,
            IMemoryCache memory, IUnitOfWork unit, IHttpContextAccessor contex, IEmailService email)
        {

            _configuration = configuration;
            _contextAccesor = contex;
            _logger = logger;
            _cache = memory;
            _unitOfWork = unit;
            _emailService = email;
        }

        // Solicitud para crear un pedido en version V1 de paypal
        #region Pagar un pedido  v2 api paypal

        public async Task<string> CreateOrderAsyncV2(Checkout pagar)
        {
            try
            {
                var order = new PaypalCreateOrder
                {
                    Intent = "CAPTURE",
                    PurchaseUnits = new List<PurchaseUnit>
                {
                new PurchaseUnit
                {
                   Amount = new AmountBase
                    {
                        CurrencyCode = pagar.currency,
                        Value = pagar.totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                        Breakdown = new Breakdown
                        {
                            ItemTotal = new MoneyOrder
                            {
                                CurrencyCode = pagar.currency,
                                Value = pagar.totalAmount.ToString("F2", CultureInfo.InvariantCulture)
                            },
                            TaxTotal = new MoneyOrder
                            {
                                CurrencyCode = pagar.currency,
                                Value = "0.00"
                            },
                            ShippingAmount = new MoneyOrder
                            {
                                CurrencyCode = pagar.currency,
                                Value = "0.00"
                            }
                        }
                    },

                    Description = "The payment transaction description.",
                    InvoiceId = Guid.NewGuid().ToString(),
                    Items = pagar.items.ConvertAll(item => new Item
                    {
                        Name = item.name,
                        Description = item.description,
                        Quantity = item.quantity.ToString(),
                        UnitAmount = new MoneyOrder
                        {
                            Value = item.price.ToString("F2", CultureInfo.InvariantCulture),
                            CurrencyCode = pagar.currency
                        },
                        Tax = new MoneyOrder
                        {
                            Value = "0.00",
                            CurrencyCode = pagar.currency
                        },
                        Sku = item.sku
                    }),
                    Shipping = new Shipping
                    {
                        Name = new NameClientOrder
                        {
                            FullName = pagar.nombreCompleto
                        },
                        Address = new Address
                        {
                            AddressLine1 = pagar.line1,
                            AddressLine2 = pagar.line2 ?? "",
                            City = pagar.ciudad,
                            State = "ES",
                            PostalCode = pagar.codigoPostal,
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
                                ReturnUrl = pagar.returnUrl,
                                CancelUrl = pagar.cancelUrl
                            }
                        }
                    }
                };

                using var httpClient = new HttpClient();

                var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
                var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");

                var authToken = await GetAccessTokenAsync(clientId, clientSecret);
                if (string.IsNullOrEmpty(authToken))
                    throw new Exception("No se pudo obtener el token de autenticación.");

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("https://api-m.sandbox.paypal.com/v2/checkout/orders", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error al crear la orden: {response.StatusCode} - {responseBody}");
                }

                var jsonResponse = JsonConvert.DeserializeObject<PayPalOrderResponse>(responseBody);
                string paymentId = jsonResponse?.id;

                if (!string.IsNullOrEmpty(paymentId))
                {
                    _cache.Set("PayPalPaymentId", paymentId, TimeSpan.FromMinutes(10));
                }

                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la orden de PayPal");
                throw new InvalidOperationException("No se pudo crear la orden de PayPal", ex);
            }
        }

        #endregion
        #region Obtener detalles del pago v2 paypal   
        public async Task<CheckoutDetails> ObtenerDetallesPagoEjecutadoV2(string id)
        {

            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            using (var httpClient = new HttpClient())
            {
                // Configura los encabezados de la solicitud
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Realiza la solicitud GET a PayPal para obtener los detalles de la suscripción
                var response = await httpClient.GetAsync($"https://api.sandbox.paypal.com/v2/checkout/orders/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Deserializa la respuesta JSON a un objeto dinámico
                    dynamic subscriptionDetails = JsonConvert.DeserializeObject<CheckoutDetails>(responseContent);
                    return subscriptionDetails;
                }
                else
                {
                    throw new Exception($"Error al obtener los detalles de la suscripción: {responseContent}");
                }
            }
        }
        #endregion
        #region Realizar reembolso pedido
        public async Task<string> RefundSaleAsync(int pedidoId, string currency)
        {
            try
            {
                // Obtener el pedido y el monto total desde el repositorio
                var (pedido, totalAmount) = await _unitOfWork.PaypalRepository.GetPedidoWithDetailsAsync(pedidoId);

                // Crear el objeto de solicitud de reembolso
                var refundRequest = new PaypalRefundResponse
                {
                    NotaParaElCliente = "Pedido cancelado por el usuario",
                    Amount =  new AmountRefund
                    {
                        Value = totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                        CurrencyCode = pedido.Currency
                    }
                };


                var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
                var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
                var authToken = await GetAccessTokenAsync(clientId, clientSecret);
                if (string.IsNullOrEmpty(authToken))
                    throw new Exception("No se pudo obtener el token de autenticación.");

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var jsonContent = new StringContent(
                        JsonConvert.SerializeObject(refundRequest, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                        Encoding.UTF8,
                        "application/json");

                    string paypalBaseUrl = "https://api-m.sandbox.paypal.com";
                    var response = await httpClient.PostAsync($"{paypalBaseUrl}/v2/payments/captures/{pedido.SaleId}/refund", jsonContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error al procesar el reembolso: {response.StatusCode} - {errorContent}");
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Reembolso exitoso: {response}", jsonResponse);
                    var refundResponse = JsonConvert.DeserializeObject<PaypalRefundResponse>(jsonResponse);
                    string refundId = refundResponse.Id;
                    // Actualizar el estado del pedido usando UnitOfWork
                    await _unitOfWork.PaypalRepository.UpdatePedidoStatusAsync(pedidoId, "Reembolsado",refundId);
                   
                    return jsonResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el reembolso");
                throw new InvalidOperationException("No se pudo realizar el reembolso", ex);
            }
        }


        #endregion
        #region Generacion token paypal

        public async Task<string> GetAccessTokenAsync(string clientId, string clientSecret)
        {
            var tokenUrl = "https://api-m.sandbox.paypal.com/v1/oauth2/token";

            using (var httpClient = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var requestBody = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" }
                };

                var content = new FormUrlEncodedContent(requestBody);
                var response = await httpClient.PostAsync(tokenUrl, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JsonConvert.DeserializeObject<dynamic>(responseString);

                return responseJson.access_token;
            }
        }
        #endregion
        #region creacion de un producto y plan de suscripcion
        public async Task<HttpResponseMessage> CreateProductAsync(string productName, string productDescription, string productType, string productCategory)
        {
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var productRequest = new CreateProductResponse
                {
                    Nombre = productName,
                    Description = productDescription,
                    Type = productType,
                    Category = productCategory,
                    Imagen = "https://example.com/product-image.jpg"
                };

                var content = new StringContent(JsonConvert.SerializeObject(productRequest), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("https://api-m.sandbox.paypal.com/v1/catalogs/products/", content);

                return response;
            }
        }



        public async Task<string> CreateSubscriptionPlanAsync(string productId, string planName, string description, decimal amount, string currency, int trialDays = 0, decimal trialAmount = 0.00m)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    var billingCycles = new List<BillingCycleDto>();

                    // Si hay días de prueba, agrega el ciclo de prueba
                    if (trialDays > 0)
                    {
                        billingCycles.Add(new BillingCycleDto
                        {
                            TenureType = "TRIAL",
                            Sequence = 1, // Primer ciclo
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
                        Sequence = trialDays > 0 ? 2 : 1, // Segundo ciclo si hay prueba, primero si no
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

                    var planRequest = new PaypalPlanDetailsDto
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

                    var content = new StringContent(JsonConvert.SerializeObject(planRequest), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("https://api-m.sandbox.paypal.com/v1/billing/plans", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        dynamic responseObject = JsonConvert.DeserializeObject(responseContent);
                        string createdPlanId = responseObject.id;

                        // Guardar los detalles del plan en la base de datos con la ID de PayPal
                        await _unitOfWork.PaypalRepository.SavePlanDetailsAsync(createdPlanId, planRequest);

                        return responseContent;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error al crear el plan de suscripción: {response.StatusCode} - {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear el plan de suscripción");
                    return $"{{\"error\":\"Se produjo un error al crear el plan de suscripción: {ex.Message}\"}}";
                }
            }
        }
        #endregion
        #region desactivar plan de suscripcion
        public async Task<string> DesactivarPlan( string planId)
        {
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                // Verificar si el plan existe antes de intentar desactivarlo
                var planResponse = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/billing/plans/{planId}");
                if (planResponse.IsSuccessStatusCode)
                {
                    // Desactivar el plan
                    var deactivatePlanResponse = await httpClient.PostAsync(
                        $"https://api-m.sandbox.paypal.com/v1/billing/plans/{planId}/deactivate",
                        null);

                    await _unitOfWork.PaypalRepository.UpdatePlanStatusInDatabase(planId, "INACTIVE");
                }
                else
                {
                    var errorContent = await planResponse.Content.ReadAsStringAsync();
                    throw new Exception($"No se pudo encontrar el plan con ID {planId}: {planResponse.StatusCode} - {errorContent}");
                }


            }
            return "Plan desactivado y producto eliminado con éxito";
        }

        #endregion
        #region Desactivar producto vinculado a un plan
        public async Task<string> MarcarDesactivadoProducto(string id)
        {
            // Validar el parámetro de entrada
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("El ID del producto no puede ser nulo o vacío.", nameof(id));
            }

            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("No se encontraron las credenciales de PayPal (ClientId o ClientSecret).");
            }

            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            try
            {
                // Verificar si el producto existe en PayPal
                var productResponse = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/catalogs/products/{id}");
                if (!productResponse.IsSuccessStatusCode)
                {
                    var errorContent = await productResponse.Content.ReadAsStringAsync();
                    throw new Exception($"No se pudo encontrar el producto con ID {id}: {productResponse.StatusCode} - {errorContent}");
                }

                // Crear la solicitud PATCH usando DTOs
                var patchRequest = new List<PatchOperation>
                {
                    new PatchOperation
                    {
                        Operation = "replace",
                        Path = "/description",
                        Value = "INACTIVO"
                    },
                    new PatchOperation
                    {
                        Operation = "replace",
                        Path = "/name",
                        Value = "INACTIVO"
                    }
                };

                // Serializar la solicitud a JSON
                var content = new StringContent(
                    JsonConvert.SerializeObject(patchRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                // Enviar la solicitud PATCH
                var patchResponse = await httpClient.PatchAsync(
                    $"https://api-m.sandbox.paypal.com/v1/catalogs/products/{id}",
                    content
                );

                if (!patchResponse.IsSuccessStatusCode)
                {
                    var errorContent = await patchResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Error al actualizar el producto con ID {id}: {patchResponse.StatusCode} - {errorContent}");
                }

                return "Producto marcado como inactivo con éxito";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar como inactivo el producto con ID {ProductId}", id);
                throw; // Relanzar la excepción para que el controlador la maneje
            }
        }
        #endregion
        #region Editar producto vinculado a un plan
        public async Task<string> EditarProducto(string id, string name, string description)
        {
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

            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("No se encontraron las credenciales de PayPal (ClientId o ClientSecret).");
            }

            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            try
            {
                // Verificar si el producto existe en PayPal
                var productResponse = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/catalogs/products/{id}");
                if (!productResponse.IsSuccessStatusCode)
                {
                    var errorContent = await productResponse.Content.ReadAsStringAsync();
                    throw new Exception($"No se pudo encontrar el producto con ID {id}: {productResponse.StatusCode} - {errorContent}");
                }

                // Crear la solicitud PATCH usando DTOs
                var patchRequest = new List<PatchOperation>
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

                // Serializar la solicitud a JSON
                var content = new StringContent(
                    JsonConvert.SerializeObject(patchRequest), // Usar patchRequest en lugar de request
                    Encoding.UTF8,
                    "application/json"
                );

                // Enviar la solicitud PATCH
                var patchResponse = await httpClient.PatchAsync(
                    $"https://api-m.sandbox.paypal.com/v1/catalogs/products/{id}",
                    content
                );

                if (!patchResponse.IsSuccessStatusCode)
                {
                    var errorContent = await patchResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Error al actualizar el producto con ID {id}: {patchResponse.StatusCode} - {errorContent}");
                }

                return "Producto actualizado con éxito";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el producto con ID {ProductId}", id);
                throw; // Relanzar la excepción para que el controlador la maneje
            }
        }
        #endregion
        #region Suscribirse a un plan
        public async Task<string> Subscribirse(string id, string returnUrl, string cancelUrl, string planName)
        {

            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var subscriptionRequest = new SubscriptionCreateResponse
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

                var content = new StringContent(JsonConvert.SerializeObject(subscriptionRequest), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("https://api-m.sandbox.paypal.com/v1/billing/subscriptions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var subscriptionJson = JsonConvert.DeserializeObject<SubscriptionCreateResponse>(responseContent);

                    var approvalLink = subscriptionJson.Links.FirstOrDefault(link => link.Rel == "approve").Href;
                    if (string.IsNullOrEmpty(approvalLink))
                    {
                        throw new Exception("No se encontró el enlace de aprobación en la respuesta de PayPal.");
                    }
                    return approvalLink;
                }
                throw new Exception("Ha ocurrido un error");
            }
        }
        #endregion
        #region Obtener detalles de la suscripción

        public async Task<PaypalSubscriptionResponse> ObtenerDetallesSuscripcion(string subscription_id)
        {
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/billing/subscriptions/{subscription_id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<PaypalSubscriptionResponse>(responseContent);
                }
                else
                {
                    throw new Exception($"Error al obtener los detalles de la suscripción: {responseContent}");
                }
            }
        }
        #endregion
        #region Obtener detalles del plan 

        public async Task<PaypalPlanResponse> ObtenerDetallesPlan(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogError("El ID del plan no puede ser nulo o vacío.");
                throw new ArgumentException("El ID del plan es requerido.");
            }

            try
            {
                // Obtén el token de acceso
                var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
                var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
                var authToken = await GetAccessTokenAsync(clientId, clientSecret);

                using (var httpClient = new HttpClient())
                {
                    // Configura los encabezados de la solicitud
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Realiza la solicitud GET a PayPal
                    var response = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/billing/plans/{id}");
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Error al obtener los detalles del plan {id}: {response.StatusCode}, {responseContent}");
                        throw new Exception($"Error al obtener los detalles del plan: {response.StatusCode}, {responseContent}");
                    }

                    // Deserializa la respuesta a PaypalPlanResponse
                    try
                    {
                        var planDetails = JsonConvert.DeserializeObject<PaypalPlanResponse>(responseContent);
                        if (planDetails == null)
                        {
                            _logger.LogError($"No se pudo deserializar la respuesta del plan {id}: {responseContent}");
                            throw new Exception("La respuesta de PayPal no contiene datos válidos.");
                        }

                        _logger.LogInformation($"Detalles del plan {id} obtenidos correctamente.");
                        return planDetails;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError($"Error al deserializar la respuesta del plan {id}: {ex.Message}, Contenido: {responseContent}");
                        throw new Exception("Error al deserializar los detalles del plan.", ex);
                    }
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

        public async Task<(PaypalProductListResponse ProductsResponse, bool HasNextPage)> GetProductsAsync(int page = 1, int pageSize = 10)
        {

            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            using (var httpClient = new HttpClient())
            {

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                string url = $"https://api.sandbox.paypal.com/v1/catalogs/products?page_size={pageSize}&page={page}";


                var response = await httpClient.GetAsync(url);


                if (response.IsSuccessStatusCode)
                {

                    var responseContent = await response.Content.ReadAsStringAsync();

                    var jsonResponse = JsonConvert.DeserializeObject<PaypalProductListResponse>(responseContent);
                    bool hasNextPage = jsonResponse.Links.Any(link => link.Rel == "next");


                    return (jsonResponse, hasNextPage);

                }
                else
                {

                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener productos: {response.StatusCode} - {errorContent}");
                }
            }
        }




        public async Task<(List<PaypalPlanResponse> plans, bool HasNextPage)> GetSubscriptionPlansAsyncV2(int page = 1, int pageSize = 6)
        {
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string url = $"https://api.sandbox.paypal.com/v1/billing/plans?page_size={pageSize}&page={page}";
                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener planes: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Respuesta JSON de /v1/billing/plans: {Content}", responseContent);

                // Deserializamos usando el DTO tipado para la lista
                var plansListResponse = JsonConvert.DeserializeObject<PaypalPlansListResponse>(responseContent);

                bool hasNextPage = plansListResponse.Links.Any(link => link.Rel == "next");

                var detailedPlans = new List<PaypalPlanResponse>();

                // Obtener detalles completos de cada plan (si quieres detalles completos)
                foreach (var plan in plansListResponse.Plans)
                {
                    try
                    {
                        string planDetailsUrl = $"https://api.sandbox.paypal.com/v1/billing/plans/{plan.Id}";
                        var planResponse = await httpClient.GetAsync(planDetailsUrl);

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
        }

        public async Task<string> CancelarSuscripcion(string subscription_id, string reason)
        {
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            using (var httpClient = new HttpClient())
            {
                // Configurar el encabezado de autorización
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Crea el contenido JSON con la razón de la cancelación
                var content = new StringContent(JsonConvert.SerializeObject(new { reason }), Encoding.UTF8, "application/json");

                // Realiza la solicitud POST a PayPal para cancelar la suscripción
                var response = await httpClient.PostAsync($"https://api-m.sandbox.paypal.com/v1/billing/subscriptions/{subscription_id}/cancel", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Verifica si la solicitud fue exitosa
                if (response.IsSuccessStatusCode)
                {
                    await ObtenerDetallesSuscripcion(subscription_id);
                    return "Suscripción cancelada exitosamente";
                }

                throw new Exception($"Error al cancelar la suscripción: {responseContent}");
            }
        }
        public async Task<string> CreateProductAndNotifyAsync(string productName, string productDescription, string productType, string productCategory)
        {
            // Crear el producto
            var response = await CreateProductAsync(productName, productDescription, productType, productCategory);

            if (response.IsSuccessStatusCode)
            {
                var email = _contextAccesor.HttpContext.User.FindFirstValue(ClaimTypes.Email);
                // Enviar el correo electrónico
                var emailDto = new EmailDto
                {
                    ToEmail = email,
                    NombreProducto = productName
                };

                await _emailService.SendEmailCreateProduct(emailDto, productName);

                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                throw new Exception($"Error al crear el producto: {response.StatusCode} - {response.Content.ReadAsStringAsync().Result}");
            }
        }
        public async Task<(string CaptureId, string Total, string Currency)> CapturarPagoAsync(string orderId)
        {
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var authToken = await GetAccessTokenAsync(clientId, clientSecret);

            if (string.IsNullOrEmpty(authToken))
                throw new Exception("No se pudo obtener el token de autenticación.");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await httpClient.PostAsync(
                $"https://api-m.sandbox.paypal.com/v2/checkout/orders/{orderId}/capture",
                new StringContent("{}", Encoding.UTF8, "application/json"));

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error al ejecutar el pago: {response.StatusCode} - {responseBody}");
            }

            var jsonResponse = JObject.Parse(responseBody);
            var captureId = jsonResponse["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["id"]?.ToString();
            var total = jsonResponse["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["amount"]?["value"]?.ToString();
            var currency = jsonResponse["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["amount"]?["currency_code"]?.ToString();

            if (string.IsNullOrEmpty(captureId) || string.IsNullOrEmpty(total) || string.IsNullOrEmpty(currency))
            {
                throw new Exception("No se pudo extraer la información del pago.");
            }

            return (captureId, total, currency);
        }

    }
}