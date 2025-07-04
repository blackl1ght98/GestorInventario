
using GestorInventario.Application.Classes;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels.paypal;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace GestorInventario.Application.Services
{
    public class PaypalServices : IPaypalService
    {

       
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly ILogger<PaypalServices> _logger;
        private readonly IMemoryCache _cache;
        
        public PaypalServices(IConfiguration configuration, GestorInventarioContext context, ILogger<PaypalServices> logger, IMemoryCache memory)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _cache = memory;
           
        }

        // Solicitud para crear un pedido en version V1 de paypal
        #region Pagar un pedido  v2 api paypal
       
        public async Task<string> CreateOrderAsyncV2(Checkout pagar)

        {

            try
            {
                var requestData = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[] 
                              {
                    new
                    {
                        amount = new
                        {
                            value = pagar.totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                            currency_code = pagar.currency,
                            breakdown = new
                            {
                                item_total = new
                                {
                                    currency_code = pagar.currency,
                                    value = pagar.totalAmount.ToString("F2", CultureInfo.InvariantCulture)
                                },
                                tax_total = new
                                {
                                    value = "0.00",
                                    currency_code = pagar.currency
                                },
                                shipping = new
                                {
                                    value = "0.00",
                                    currency_code = pagar.currency
                                }
                            }
                        },
                        description = "The payment transaction description.",
                        invoice_id = Guid.NewGuid().ToString(),
                        items = pagar.items.ConvertAll(item => new
                        {
                            name = item.name,
                            description = item.description,
                            quantity = item.quantity.ToString(),
                            unit_amount = new
                            {
                                value = item.price.ToString("F2", CultureInfo.InvariantCulture),
                                currency_code = pagar.currency
                            },
                            tax = new
                            {
                                value = "0.00",
                                currency_code = pagar.currency
                            },
                            sku = item.sku
                        }),
                        shipping = new
                        {
                            name = new
                            {
                                full_name = pagar.nombreCompleto
                            },
                            address = new
                            {
                                address_line_1 = pagar.line1,
                                address_line_2 = pagar.line2 ?? "",
                                admin_area_2 = pagar.ciudad,
                                admin_area_1 = "ES",
                                postal_code = pagar.codigoPostal,
                                country_code = "ES"
                            }
                        }
                    }
                },
                    payment_source = new
                    {
                        paypal = new
                        {
                            experience_context = new
                            {
                                payment_method_preference = "IMMEDIATE_PAYMENT_REQUIRED",
                                return_url = pagar.returnUrl,
                                cancel_url = pagar.cancelUrl
                            }
                        }
                    }
                };


                using (var httpClient = new HttpClient())
                {
                    var authToken = await GetAccessTokenAsync();
                    if (string.IsNullOrEmpty(authToken))
                        throw new Exception("No se pudo obtener el token de autenticación.");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Convertir la solicitud a formato JSON
                    var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

                    // Enviar la solicitud POST a la API v1 de PayPal
                    var response = await httpClient.PostAsync("https://api-m.sandbox.paypal.com/v2/checkout/orders", content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Error al crear la orden: {response.StatusCode} - {responseBody}");
                    }

                    // Extraer el ID del pago de la respuesta JSON
                    var jsonResponse = JsonConvert.DeserializeObject<PayPalOrderResponse>(responseBody);
                    string paymentId = jsonResponse.id;

                    // Guardar el ID en caché temporalmente
                    _cache.Set("PayPalPaymentId", paymentId, TimeSpan.FromMinutes(10));

                    return responseBody;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la orden de PayPal");
                throw new InvalidOperationException("No se pudo crear la orden de PayPal", ex);
            }
        }
        #endregion
        #region Obtener detalles del pago v2 paypal
       
        public async Task<dynamic> ObtenerDetallesPagoEjecutadoV2(string id)
        {
            
            var authToken = await GetAccessTokenAsync();

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
                    dynamic subscriptionDetails = JsonConvert.DeserializeObject(responseContent);
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
                var pedido = await _context.Pedidos
                    .Include(p => p.DetallePedidos)
                    .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(p => p.Id == pedidoId);

                if (pedido == null || string.IsNullOrEmpty(pedido.SaleId))
                    throw new ArgumentException("Pedido no encontrado o SaleId no disponible.");

                decimal totalAmount = pedido.DetallePedidos.Sum(d => d.Producto.Precio * (d.Cantidad ?? 0));

                if (string.IsNullOrEmpty(pedido.Currency))
                    throw new ArgumentException("El código de moneda no está definido.");

                // Crea el objeto de solicitud de reembolso con estructura v2
                var refundRequest = new
                {
                    amount = new
                    {
                        value = totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                        currency_code = pedido.Currency
                    }
                };

                var authToken = await GetAccessTokenAsync();
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

                    pedido.EstadoPedido = "Reembolsado";
                    _context.Update(pedido);
                    await _context.SaveChangesAsync();

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
        /*Cuando no sea posible hacer alguna operacion en paypal como es el caso de las susbcripciones que el paquete NuGet de Paypal no dispone de todos los metodos es necesario construir la peticion entera y para ello
         * hay que seguir al pie de la letra la documentacion de paypal.*/
        public async Task<string> GetAccessTokenAsync()
        {
          
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            //Este enlace es lo que se necesita para autenticarnos contra paypal
            var tokenUrl = "https://api-m.sandbox.paypal.com/v1/oauth2/token";
            //Como necesitamos manipular la peticion  hacemos uso de httpclient
            using (var httpClient = new HttpClient())
            {
                //Convertimos a un array de bytes el clientid y el clientsecret
                var byteArray = Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
                //Le pasamos a la cabecera de la peticion lo siguiente:
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                //Almacenamos en un diccionario las credenciales del usuario
                var requestBody = new Dictionary<string, string>
                 {
                    { "grant_type", "client_credentials" }
                  };
                //Lee la url codificada
                var content = new FormUrlEncodedContent(requestBody);
                //Realiza la peticion post al servidor de paypal
                var response = await httpClient.PostAsync(tokenUrl, content);
                //La peticion si es exitosa continua ejecutandose
                response.EnsureSuccessStatusCode();
                //Lee la respuesta devuelta por paypal
                var responseString = await response.Content.ReadAsStringAsync();
                //Deserializa el json devuelto por el servidor de paypal
                var responseJson = JsonConvert.DeserializeObject<dynamic>(responseString);
                //finalmente obtenemos el token
                return responseJson.access_token;
            }
        }
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
            var authToken = await GetAccessTokenAsync();

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var productRequest = new
                {
                    name = productName,
                    description = productDescription,
                    type = productType,
                    category = productCategory,
                    image_url = "https://example.com/product-image.jpg"
                };

                var content = new StringContent(JsonConvert.SerializeObject(productRequest), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("https://api-m.sandbox.paypal.com/v1/catalogs/products/", content);

                return response; 
            }
        }



        public async Task<string> CreateSubscriptionPlanAsync(string productId, string planName, string description, decimal amount, string currency, int trialDays = 0, decimal trialAmount = 0.00m)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            var authToken = await GetAccessTokenAsync();

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
               
                try
                {
                    /*Como no se sabe que tipo de dato viene en el json se pone que es de tipo object*/
                    var billingCycles = new List<object>();

                    // Si hay días de prueba, agrega el ciclo de prueba
                    if (trialDays > 0)
                    {
                        //Como lo primero que encontramos es un array la forma de ponerlo es asi
                        billingCycles.Add(new
                        {
                            //Dentro de este array podemos tener objetos con propiedades la forma de ponerlo es asi
                            frequency = new
                            {
                                interval_unit = "DAY", //valores admitidos DAY,WEEK,MONTH,YEAR
                                interval_count = trialDays
                            },
                            tenure_type = "TRIAL", //Valores admitidos TRIAL O REGULAR
                            sequence = 1,
                            total_cycles = 1,
                            pricing_scheme = new
                            {
                                fixed_price = new
                                {
                                    value = trialAmount.ToString("0.00", CultureInfo.InvariantCulture),
                                    currency_code = currency
                                }
                            }
                        });
                    }

                    // Agrega el ciclo regular
                    billingCycles.Add(new
                    {
                        frequency = new
                        {
                            interval_unit = "MONTH",
                            interval_count = 1
                        },
                        tenure_type = "REGULAR",
                        sequence = billingCycles.Count + 1, // Este valor será 1 si no hay período de prueba, o 2 si lo hay
                        total_cycles = 12,
                        pricing_scheme = new
                        {
                            fixed_price = new
                            {
                                value = amount.ToString("0.00", CultureInfo.InvariantCulture),
                                currency_code = currency
                            }
                        }
                    });

                    var planRequest = new
                    {
                        product_id = productId,
                        name = planName,
                        description = description,
                        status = "ACTIVE",
                        billing_cycles = billingCycles,
                        payment_preferences = new
                        {
                            auto_bill_outstanding = true,
                            setup_fee = new
                            {
                                value = "0.00",
                                currency_code = currency
                            },
                            setup_fee_failure_action = "CONTINUE",
                            payment_failure_threshold = 3
                        },
                        taxes = new
                        {
                            percentage = "10",
                            inclusive = false
                        }
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(planRequest), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("https://api-m.sandbox.paypal.com/v1/billing/plans", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        dynamic responseObject = JsonConvert.DeserializeObject(responseContent);

                        // Obtener la ID del plan creado desde la respuesta de PayPal
                        string createdPlanId = responseObject.id;

                        // Guardar los detalles del plan en la base de datos con la ID de PayPal
                        await SavePlanDetailsToDatabase(createdPlanId, planRequest);

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
                    // Aquí manejamos el error para evitar que se propague y estropee el flujo
                    Console.WriteLine($"Error controlado al crear el plan de suscripción: {ex.Message}");

                    // Opcionalmente, puedes devolver un mensaje personalizado
                    return $"{{\"error\":\"Se produjo un error al crear el plan de suscripción: {ex.Message}\"}}";
                }
            }
        }


        private async Task SavePlanDetailsToDatabase(string createdPlanId, dynamic planRequest)
        {
            var planDetails = new PlanDetail
            {
                Id = Guid.NewGuid().ToString(),
                PaypalPlanId = createdPlanId,
                ProductId = planRequest.product_id,
                Name = planRequest.name,
                Description = planRequest.description,
                Status = planRequest.status,
                AutoBillOutstanding = planRequest.payment_preferences.auto_bill_outstanding,
                SetupFee = decimal.Parse(planRequest.payment_preferences.setup_fee.value, CultureInfo.InvariantCulture),
                SetupFeeFailureAction = planRequest.payment_preferences.setup_fee_failure_action,
                PaymentFailureThreshold = planRequest.payment_preferences.payment_failure_threshold,
                TaxPercentage = decimal.Parse(planRequest.taxes.percentage, CultureInfo.InvariantCulture),
                TaxInclusive = planRequest.taxes.inclusive
            };

            // Verificar si existe un ciclo de facturación de prueba
            if (planRequest.billing_cycles.Count > 1)
            {
                planDetails.TrialIntervalUnit = planRequest.billing_cycles[0].frequency.interval_unit;
                planDetails.TrialIntervalCount = planRequest.billing_cycles[0].frequency.interval_count;
                planDetails.TrialTotalCycles = planRequest.billing_cycles[0].total_cycles;
                planDetails.TrialFixedPrice = decimal.Parse(planRequest.billing_cycles[0].pricing_scheme.fixed_price.value, CultureInfo.InvariantCulture);

                // Información del ciclo regular
                planDetails.RegularIntervalUnit = planRequest.billing_cycles[1].frequency.interval_unit;
                planDetails.RegularIntervalCount = planRequest.billing_cycles[1].frequency.interval_count;
                planDetails.RegularTotalCycles = planRequest.billing_cycles[1].total_cycles;
                planDetails.RegularFixedPrice = decimal.Parse(planRequest.billing_cycles[1].pricing_scheme.fixed_price.value, CultureInfo.InvariantCulture);
            }
            else if (planRequest.billing_cycles.Count == 1)
            {
                // Solo hay ciclo regular
                planDetails.RegularIntervalUnit = planRequest.billing_cycles[0].frequency.interval_unit;
                planDetails.RegularIntervalCount = planRequest.billing_cycles[0].frequency.interval_count;
                planDetails.RegularTotalCycles = planRequest.billing_cycles[0].total_cycles;
                planDetails.RegularFixedPrice = decimal.Parse(planRequest.billing_cycles[0].pricing_scheme.fixed_price.value, CultureInfo.InvariantCulture);
            }

            _context.PlanDetails.Add(planDetails);
            await _context.SaveChangesAsync();
        }
        #endregion
        #region desactivar plan de suscripcion
        public async Task<string> DesactivarPlan(string productId, string planId)
        {
            var authToken = await GetAccessTokenAsync();
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
                   
                    await UpdatePlanStatusInDatabase(planId, "INACTIVE");
                }
                else
                {
                    var errorContent = await planResponse.Content.ReadAsStringAsync();
                    throw new Exception($"No se pudo encontrar el plan con ID {planId}: {planResponse.StatusCode} - {errorContent}");
                }






            }
            return "Plan desactivado y producto eliminado con éxito";
        }
        private async Task UpdatePlanStatusInDatabase(string planId, string status)
        {
            var planDetails = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);
            if (planDetails != null)
            {
                planDetails.Status = status;
                _context.PlanDetails.Update(planDetails);
                await _context.SaveChangesAsync();
            }
        }
        #endregion
        #region Desactivar producto vinculado a un plan
        public async Task<string> MarcarDesactivadoProducto(string id)
        {
            var authToken = await GetAccessTokenAsync();
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                var productResponse = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/catalogs/products/{id}");
                if (!productResponse.IsSuccessStatusCode)
                {
                    var errorContent = await productResponse.Content.ReadAsStringAsync();
                    throw new Exception($"No se pudo encontrar el producto con ID {id}: {productResponse.StatusCode} - {errorContent}");
                }
                var request = new[]
                {
                   new
                   {
                       op="replace",
                       path="/description",
                       value="INACTIVO"
                   },
                     new
                   {
                       op="replace",
                       path="/name",
                       value="INACTIVO"
                   }

                };
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var deleteProduct = await httpClient.PatchAsync($"https://api-m.sandbox.paypal.com/v1/catalogs/products/{id}", content);
                //Si la peticion es correcta..
                if (deleteProduct.IsSuccessStatusCode)
                {
                    //Lee el contenido de la peticion
                    var responseContent = await deleteProduct.Content.ReadAsStringAsync();
                   
                    return responseContent;
                }
                else
                {
                    var errorContent = await deleteProduct.Content.ReadAsStringAsync();
                    throw new Exception($"Error al eliminar el producto: {deleteProduct.StatusCode} - {errorContent}");
                }

            }


        }
        #endregion
        #region Editar producto vinculado a un plan
        public async Task<string> EditarProducto(string id, string name, string description)
        {
            var authToken = await GetAccessTokenAsync();
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                var productResponse = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/catalogs/products/{id}");
                if (!productResponse.IsSuccessStatusCode)
                {
                    var errorContent = await productResponse.Content.ReadAsStringAsync();
                    throw new Exception($"No se pudo encontrar el producto con ID {id}: {productResponse.StatusCode} - {errorContent}");
                }
                var request = new[]
                {
                      new
                   {
                       op="replace",
                       path="/name",
                       value=name
                   },
                   new
                   {
                       op="replace",
                       path="/description",
                       value=description
                   }


                };
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var deleteProduct = await httpClient.PatchAsync($"https://api-m.sandbox.paypal.com/v1/catalogs/products/{id}", content);
                //Si la peticion es correcta..
                if (deleteProduct.IsSuccessStatusCode)
                {
                
                    var responseContent = await deleteProduct.Content.ReadAsStringAsync();
                   
                    return responseContent;
                }
                else
                {
                    var errorContent = await deleteProduct.Content.ReadAsStringAsync();
                    throw new Exception($"Error al eliminar el producto: {deleteProduct.StatusCode} - {errorContent}");
                }
            }
        }
        #endregion
        #region Suscribirse a un plan
        public async Task<string> Subscribirse(string id, string returnUrl, string cancelUrl, string planName)
        {
            
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
         
            var authToken = await GetAccessTokenAsync(); 
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var subscriptionRequest = new
                {
                    plan_id = id,
                    application_context = new
                    {
                        brand_name = planName,
                        locale = "es-ES",
                        shipping_preference = "NO_SHIPPING",
                        user_action = "SUBSCRIBE_NOW",
                        payment_method = new
                        {
                            payer_selected = "PAYPAL",
                            payee_preferred = "IMMEDIATE_PAYMENT_REQUIRED"
                        },
                        return_url = returnUrl,
                        cancel_url = cancelUrl
                    }
                };

                var content = new StringContent(JsonConvert.SerializeObject(subscriptionRequest), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("https://api-m.sandbox.paypal.com/v1/billing/subscriptions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    dynamic subscriptionJson = JsonConvert.DeserializeObject(responseContent);

                    var approvalLink = ((IEnumerable<dynamic>)subscriptionJson.links).First(link => link.rel == "approve").href;

                    return approvalLink;
                }
                throw new Exception("Ha ocurrido un error");
            }
        }
        #endregion
        #region Obtener detalles de la suscripción
        public async Task<dynamic> ObtenerDetallesSuscripcion(string subscription_id)
        {
           
            var authToken = await GetAccessTokenAsync();

            using (var httpClient = new HttpClient())
            {
               
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

              
                var response = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/billing/subscriptions/{subscription_id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    
                    dynamic subscriptionDetails = JsonConvert.DeserializeObject(responseContent);
                    return subscriptionDetails;
                }
                else
                {
                    throw new Exception($"Error al obtener los detalles de la suscripción: {responseContent}");
                }
            }
        }
        #endregion
        #region Obtener detalles del plan 
        public async Task<dynamic> ObtenerDetallesPlan(string id)
        {
            // Obtén el token de acceso
            var authToken = await GetAccessTokenAsync();

            using (var httpClient = new HttpClient())
            {
                // Configura los encabezados de la solicitud
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Realiza la solicitud GET a PayPal para obtener los detalles de la suscripción
                var response = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/billing/plans/{id}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Deserializa la respuesta JSON a un objeto dinámico
                    dynamic subscriptionDetails = JsonConvert.DeserializeObject(responseContent);
                    return subscriptionDetails;
                }
                else
                {
                    throw new Exception($"Error al obtener los detalles de la suscripción: {responseContent}");
                }
            }
        }
        #endregion
       
        public async Task<(string ProductsResponse, bool HasNextPage)> GetProductsAsync(int page = 1, int pageSize = 10)
        {
          
            var authToken = await GetAccessTokenAsync();

            using (var httpClient = new HttpClient())
            {
               
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

               
                string url = $"https://api.sandbox.paypal.com/v1/catalogs/products?page_size={pageSize}&page={page}";

            
                var response = await httpClient.GetAsync(url);

            
                if (response.IsSuccessStatusCode)
                {
                
                    var responseContent = await response.Content.ReadAsStringAsync();

                   
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);

                    // Como jsonResponse es de tipo dynamic, realizamos una conversión explícita a IEnumerable para manejar listas o arrays,
                    // en este caso, la lista de enlaces ("links") que devuelve la API de PayPal
                    var links = ((IEnumerable<dynamic>)jsonResponse.links).ToList();

                    // Verificamos si existe un enlace que indique la presencia de una página siguiente ("next")
                    bool hasNextPage = links.Any(link => link.rel == "next");

                 
                    return (responseContent, hasNextPage);
                }
                else
                {
                   
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener productos: {response.StatusCode} - {errorContent}");
                }
            }
        }

        public async Task<(List<Plan> plans, bool HasNextPage)> GetSubscriptionPlansAsync(int page = 1, int pageSize = 6)
        {
            var authToken = await GetAccessTokenAsync();

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Paso 1: Obtener la lista de planes
                string url = $"https://api.sandbox.paypal.com/v1/billing/plans?page_size={pageSize}&page={page}";
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al obtener planes: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                var links = ((IEnumerable<dynamic>)jsonResponse.links).ToList();
                bool hasNextPage = links.Any(link => link.rel == "next");

                // Paso 2: Deserializar la lista de planes
                var plansList = JsonConvert.DeserializeObject<PlansResponse>(responseContent);
                var plans = plansList.Plans ?? new List<Plan>();

                // Paso 3: Obtener los detalles completos de cada plan
                foreach (var plan in plans)
                {
                    string planDetailsUrl = $"https://api.sandbox.paypal.com/v1/billing/plans/{plan.id}";
                    var planResponse = await httpClient.GetAsync(planDetailsUrl);
                    if (planResponse.IsSuccessStatusCode)
                    {
                        var planDetailsContent = await planResponse.Content.ReadAsStringAsync();
                        var planDetails = JsonConvert.DeserializeObject<Plan>(planDetailsContent);

                        // Actualizar el plan con los detalles completos
                        plan.billing_cycles = planDetails.billing_cycles;
                        plan.taxes = planDetails.taxes;
                        // También puedes actualizar otros campos si es necesario
                        plan.description = planDetails.description ?? plan.description;
                        plan.status = planDetails.status ?? plan.status;
                    }
                    else
                    {
                        var errorContent = await planResponse.Content.ReadAsStringAsync();
                        throw new Exception($"Error al obtener detalles del plan {plan.id}: {planResponse.StatusCode} - {errorContent}");
                    }
                }
                Console.WriteLine("Planes en página " + page + ": " + plans.Count);
               
                return (plans, hasNextPage);
            }
        }

        public async Task<string> CancelarSuscripcion(string subscription_id, string reason)
        {
            var authToken = await GetAccessTokenAsync(); 

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
    }
}