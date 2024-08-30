using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PayPal;
using PayPal.Api;
using Polly;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;

namespace GestorInventario.Application.Services
{
    public class PaypalServices : IPaypalService
    {
        
        private readonly APIContext _apiContext;
        private readonly Payment _payment;
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly ILogger<PaypalServices> _logger;
        public PaypalServices(IConfiguration configuration, GestorInventarioContext context, ILogger<PaypalServices> logger)
        {
           
            _configuration = configuration;
            _context = context;
            _logger = logger;
          
            var clientId = configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSeecret = configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            var mode = configuration["Paypal:Mode"] ?? Environment.GetEnvironmentVariable("Paypal_Mode");
           
            var config = new Dictionary<string, string>
            {
                {"mode",mode },
                {"clientId",clientId },
                {"clientSecret", clientSeecret}
            };
            //Esta es la manera correcta de obtener el token de acceso
            var accessToken = new OAuthTokenCredential(clientId, clientSeecret, config).GetAccessToken();
            //Cada vez que se llame a apicontext este contendra el token de acceso a paypal lo cual nos permite opera con la api de paypal
            _apiContext = new APIContext(accessToken);
            //En esta parte de la configuracion asignamos el metodo de pago a usar
            _payment = new Payment
            {
                intent = "sale",
                payer = new Payer { payment_method = "paypal" }
            };

        }
        public async Task<Payment> CreateDonation(decimal amount, string returnUrl, string cancelUrl, string currency)
        {
          
        
            var itemList = new ItemList()
            {
                items = new List<Item>()
                {
                    new Item()
                    {
                        name="Donacion",
                        currency=currency,
                        price= amount.ToString("0.00"),
                        quantity="1",
                        sku="donacion"
                    }

                }
            };
            var transaction = new Transaction()
            {
                amount = new Amount()
                {
                    currency = currency,
                    total = amount.ToString("0.00"),
                    details = new Details()
                    {
                        subtotal = amount.ToString("0.00")
                    },

                },

                item_list = itemList,
                description = "Donacion"

            };
            var payment = new Payment()
            {
                intent = "sale",
                payer = new Payer() { payment_method = "paypal" },
                redirect_urls = new RedirectUrls()
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl,
                },
                transactions = new List<Transaction>() { transaction }
            };
            var settings = new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture
            };
            var createdPayment = payment.Create(_apiContext);
            return createdPayment;
        }
        // Este es el método que se utiliza para crear un pedido en PayPal.
        public async Task<Payment> CreateOrderAsync(List<Item> items, decimal amount, string returnUrl, string cancelUrl, string currency)
        {
            
            // Aquí se crea una nueva instancia de ItemList con los items pasados al método.
            var itemList = new ItemList()
            {
                items = items
            };
            // Aquí se crea una nueva instancia de Transaction con la cantidad, la lista de items y la descripción.
            var transaction = new Transaction()
            {
                amount = new Amount()
                {
                    currency = currency,
                    total = amount.ToString("0.00"),
                    details = new Details()
                    {
                        subtotal = amount.ToString("0.00")
                    },

                },

                item_list = itemList,
                description = "Aquisicion de productos"

            };
            // Aquí se crea una nueva instancia de Payment con la intención de "sale", el pagador, las URL de redirección y las transacciones.
            var payment = new Payment()
            {
                intent = "sale",//Para obtener un id de autorizacion de pago tenemos que poner aqui "authorize"
                payer = new Payer() { payment_method = "paypal" },
                redirect_urls = new RedirectUrls()
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl,
                },
                transactions = new List<Transaction>() { transaction }
            };
            var settings = new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture
            };
            // Aquí se crea el pago en PayPal y se devuelve el pago creado.
            var createdPayment = payment.Create(_apiContext);
            return createdPayment;
        }
        /*
         * SINTAXIS ANTIGUA PARA REALIZAR UN REEMBOLSO
        public async Task<Refund> RefundSaleAsync(int pedidoId, decimal refundAmount = 0, string currency = "EUR")
        {
            try
            {
                // Recuperar el pedido de la base de datos
                var pedido = await _context.Pedidos.FindAsync(pedidoId);

                if (pedido == null || string.IsNullOrEmpty(pedido.SaleId))
                {
                    throw new ArgumentException("Pedido no encontrado o SaleId no disponible.");
                }
                var saleId = pedido.SaleId;

                // Crear la solicitud de reembolso
                var refundRequest = new RefundRequest();
                if (refundAmount > 0)
                {
                    refundRequest.amount = new Amount
                    {
                        total = refundAmount.ToString("0.00"),
                        currency = currency
                    };
                }
                // Ejecutar el reembolso utilizando RefundTransaction
                var refund = new Refund();
                var sale = new Sale() { id = saleId };
                var response = sale.Refund(_apiContext, refund);

                // Actualizar el estado del pedido a "Reembolsado"
                pedido.EstadoPedido = "Reembolsado";
                _context.Update(pedido);
                await _context.SaveChangesAsync();

                return refund;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el reembolso");
                throw new InvalidOperationException("No se pudo realizar el reembolso", ex);
            }
        }
        */
        public async Task<Refund> RefundSaleAsync(int pedidoId, string currency)
        {
            try
            {
               //Para que el proceso de reembolso sea lo mas sencillo posible hacemos una consulta directa a base de datos que esta tendra los datos necesarios para devolver el pedido
                var pedido = await _context.Pedidos
                 .Include(p => p.DetallePedidos)
                 .ThenInclude(d => d.Producto)  
                 .FirstOrDefaultAsync(p => p.Id == pedidoId);


                if (pedido == null || string.IsNullOrEmpty(pedido.SaleId))
                {
                    throw new ArgumentException("Pedido no encontrado o SaleId no disponible.");
                }

                // Calcular el monto total del pedido

                decimal totalAmount = pedido.DetallePedidos.Sum(d => (d.Producto.Precio * (d.Cantidad ?? 0)));




                var saleId = pedido.SaleId;

                // Crear la solicitud de reembolso
                var refundRequest = new RefundRequest
                {
                    amount = new Amount
                    {
                        total = totalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                        currency = pedido.Currency
                    }
                };

                // Ejecutar el reembolso utilizando la nueva sintaxis
                var refund = Sale.Refund(_apiContext, saleId, refundRequest);

                // Actualizar el estado del pedido a "Reembolsado"
                pedido.EstadoPedido = "Reembolsado";
                _context.Update(pedido);
                await _context.SaveChangesAsync();

                return refund;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el reembolso");
                throw new InvalidOperationException("No se pudo realizar el reembolso", ex);
            }
        }


        /*Cuando no sea posible hacer alguna operacion en paypal como es el caso de las susbcripciones que el paquete NuGet de Paypal no dispone de todos los metodos es necesario construir la peticion entera y para ello
         * hay que seguir al pie de la letra la documentacion de paypal.*/
        public async Task<string> GetAccessTokenAsync()
        {
            //Como las otras acciones se obtiene el cliente-id y el secreto del cliente 
            var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
            var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
            //Este enlace es lo que se necesita para "autenticarnos" en paypal o mas bien pasarle el token a paypal
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

                return response; // Devuelve HttpResponseMessage en lugar de string
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
                    var billingCycles = new List<object>();

                    // Si hay días de prueba, agrega el ciclo de prueba
                    if (trialDays > 0)
                    {
                        billingCycles.Add(new
                        {
                            frequency = new
                            {
                                interval_unit = "DAY",
                                interval_count = trialDays
                            },
                            tenure_type = "TRIAL",
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
                        null); // No se necesita cuerpo en la solicitud
                    //REVISAR 
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
                    //Devuelve el contenido de esa peticion
                    return responseContent;
                }
                else
                {
                    var errorContent = await deleteProduct.Content.ReadAsStringAsync();
                    throw new Exception($"Error al eliminar el producto: {deleteProduct.StatusCode} - {errorContent}");
                }

            }


        }
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
                    //Lee el contenido de la peticion
                    var responseContent = await deleteProduct.Content.ReadAsStringAsync();
                    //Devuelve el contenido de esa peticion
                    return responseContent;
                }
                else
                {
                    var errorContent = await deleteProduct.Content.ReadAsStringAsync();
                    throw new Exception($"Error al eliminar el producto: {deleteProduct.StatusCode} - {errorContent}");
                }
            }
        }
        public async Task<string> Subscribirse(string id, string returnUrl, string cancelUrl, string planName)
        {
            // Cambia la cultura actual del hilo a InvariantCulture
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            // Cambia la cultura de la interfaz de usuario actual del hilo a InvariantCulture
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            //Obtenemos el token de acceso
            var authToken = await GetAccessTokenAsync(); // Obtén el token de acceso
            //Como vamos ha hacer una peticion a paypal que no esta programado en el paquete de paypal tenemos que contruir la peticion para ello usamos httpclient
            using (var httpClient = new HttpClient())
            {
                //A la peticion le pasamos el token 
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

                // Realiza la solicitud POST a PayPal
                var response = await httpClient.PostAsync("https://api-m.sandbox.paypal.com/v1/billing/subscriptions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Verifica si la solicitud fue exitosa
                if (response.IsSuccessStatusCode)
                {
                    dynamic subscriptionJson = JsonConvert.DeserializeObject(responseContent);

                    // Obtén la URL de aprobación de PayPal
                    var approvalLink = ((IEnumerable<dynamic>)subscriptionJson.links).First(link => link.rel == "approve").href;

                    // Redirige al usuario a la URL de aprobación de PayPal
                    return approvalLink;
                }
                throw new Exception("Ha ocurrido un error");
            }
        }
        public async Task<dynamic> ObtenerDetallesSuscripcion(string subscription_id)
        {
            // Obtén el token de acceso
            var authToken = await GetAccessTokenAsync();

            using (var httpClient = new HttpClient())
            {
                // Configura los encabezados de la solicitud
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Realiza la solicitud GET a PayPal para obtener los detalles de la suscripción
                var response = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/billing/subscriptions/{subscription_id}");
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
        public async Task<dynamic> ObtenerDetallesPagoEjecutado(string id)
        {
            // Obtén el token de acceso
            var authToken = await GetAccessTokenAsync();

            using (var httpClient = new HttpClient())
            {
                // Configura los encabezados de la solicitud
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Realiza la solicitud GET a PayPal para obtener los detalles de la suscripción
                var response = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/payments/payment/{id}");
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







    }
}