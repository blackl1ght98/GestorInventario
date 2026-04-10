using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Response.PayPal;
using GestorInventario.Application.DTOs.Response_paypal;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Application.DTOs.Response_paypal.PATCH;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Application.Exceptions;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.External_Sevices
{
    public class PaypalSubscriptionService: IPaypalSubscriptionService
    {
        private readonly ILogger<PaypalSubscriptionService> _logger;

        private readonly IPaypalRepository _repo;
        private readonly IPayPalHttpClient _paypal;
        private readonly CultureHelper _culture;

        public PaypalSubscriptionService(ILogger<PaypalSubscriptionService> logger,
           IPayPalHttpClient paypal, IPaypalRepository repo, CultureHelper culture)
        {
            _logger = logger;
            _culture = culture;
            _paypal = paypal;
            _repo = repo;
        }
        #region creacion de un producto y plan de suscripcion
        public async Task<CreateProductResponseDto> CreateProductAsync(string productName, string productDescription, string productType, string productCategory)
        {

            var productRequest = BuildProductRequest(productName, productDescription, productType, productCategory);
            var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Post,
                $"v1/catalogs/products/",
                productRequest,
                async error =>
                {
                    var errorBody = await error.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Error al crear el producto {error.StatusCode} - {errorBody}");
                });
            var productResponse = JsonConvert.DeserializeObject<CreateProductResponseDto>(responseBody);
            if (productResponse == null)
            {
                throw new ArgumentNullException("No se pudo obtener el producto");
            }
            return productResponse;
        }

        // Update the BuildProductRequest to use the request DTO
        private CreateProductRequestDto BuildProductRequest(string productName, string productDescription, string productType, string productCategory)
        {
            return new CreateProductRequestDto
            {
                Nombre = productName,
                Description = productDescription,
                Type = productType,
                Category = productCategory,
                Imagen = "https://example.com/product-image.jpg"
            };
        }

        public async Task<string> CreateSubscriptionPlanAsync(string productId, string planName, string description, decimal amount, string currency,string intervalUnit, int trialDays = 0, decimal trialAmount = 0.00m)
        {
            _culture.SetInvariantCultureSafe();
            try
            {
                var planRequest = BuildPaypalPlanRequest(productId, planName, description, amount, currency, trialDays, trialAmount,intervalUnit);
                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    "v1/billing/plans",
                    planRequest,
                    async err =>
                    {
                        var errorBody = await err.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al crear la suscripcion {err.StatusCode} - {errorBody}");
                    });
                var responseObject = JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(responseBody);
                string createdPlanId = responseObject.Id;
                // Guardar los detalles del plan en la base de datos con la ID de PayPal
                await _repo.SavePlanDetailsAsync(createdPlanId, planRequest);
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el plan de suscripción");
                return $"{{\"error\":\"Se produjo un error al crear el plan de suscripción: {ex.Message}\"}}";
            }
        }
        private PaypalPlanDetailsDto BuildPaypalPlanRequest(
          string productId,
          string planName,
          string description,
          decimal amount,
          string currency,
          int trialDays = 0,
          decimal trialAmount = 0.00m,
          string intervalUnit = "MONTH")   // ← Nuevo parámetro
        {
            var billingCycles = new List<BillingCycleDto>();

            // Ciclo de prueba (si existe)
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

            // Ciclo regular (dinámico: MONTH o YEAR)
            int sequence = trialDays > 0 ? 2 : 1;
            int totalCycles = intervalUnit == "YEAR" ? 1 : 12;   // 1 año o 12 meses
                                                                 // Ciclo regular (dinámico: MONTH o YEAR)
            billingCycles.Add(new BillingCycleDto
            {
                TenureType = "REGULAR",
                Sequence = trialDays > 0 ? 2 : 1,
                Frequency = new FrequencyDto
                {
                    IntervalUnit = intervalUnit,        // "MONTH" o "YEAR"
                    IntervalCount = 1
                },
                TotalCycles = intervalUnit == "YEAR" ? 0 : 12,   // ← Aquí está el cambio importante
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
                    SetupFee = new FixedPriceDto { Value = "0.00", CurrencyCode = currency },
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
        public async Task<PaypalPlanResponseDto> ObtenerDetallesPlan(string id)
        {

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogError("El ID del plan no puede ser nulo o vacío.");
                throw new ArgumentException("El ID del plan es requerido.");
            }
            try
            {

                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Get,
                    $"v1/billing/plans/{id}",
                    async error =>
                    {
                        var errBody = await error.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al obtener los detalles del plan: {error.StatusCode} - {errBody}");
                    });
                // Deserializa la respuesta a PaypalPlanResponse           
                var planDetails = JsonConvert.DeserializeObject<PaypalPlanResponseDto>(responseBody);
                if (planDetails == null)
                {
                    _logger.LogError($"No se pudo deserializar la respuesta del plan {id}: {responseBody}");
                    throw new Exception("La respuesta de PayPal no contiene datos válidos.");
                }
                _logger.LogInformation($"Detalles del plan {id} obtenidos correctamente.");
                return planDetails;
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
        public async Task<(PaypalProductListResponseDto ProductsResponse, bool HasNextPage)> GetProductsAsync(int page = 1, int pageSize = 10)
        {

            var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Get,
                $"v1/catalogs/products?page_size={pageSize}&page={page}",
                async err =>
                {
                    var errorBody = await err.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Error al obtener los productos {err.StatusCode} - {errorBody} ");
                });
            var jsonResponse = JsonConvert.DeserializeObject<PaypalProductListResponseDto>(responseBody);
            if (jsonResponse == null)
            {
                throw new ArgumentNullException("No se ha podido obtener los productos");

            }
            bool hasNextPage = jsonResponse.Links.Any(link => link.Rel == "next");
            return (jsonResponse, hasNextPage);
        }
        #endregion

        #region Obtener Planes de suscripcion
        public async Task<(List<PaypalPlanResponseDto> plans, bool HasNextPage)> GetSubscriptionPlansAsyncV2(int page = 1, int pageSize = 6)
        {


            var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Get,
                $"v1/billing/plans?page_size={pageSize}&page={page}",
                async err =>
                {
                    var errorBody = await err.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Error al obtener los planes");
                });
            // Deserializamos usando el DTO tipado para la lista
            var plansListResponse = JsonConvert.DeserializeObject<PaypalPlansListResponse>(responseBody);
            if (plansListResponse == null)
            {
                throw new ArgumentNullException("No se pudo obtener los planes");
            }
            bool hasNextPage = plansListResponse.Links.Any(link => link.Rel == "next");
            var detailedPlans = new List<PaypalPlanResponseDto>();
            // Obtener detalles completos de cada plan (si quieres detalles completos)
            foreach (var plan in plansListResponse.Plans)
            {
                try
                {
                    var requestPlanDetails = await _paypal.ExecutePayPalRequestAsync<string>(
                        HttpMethod.Get,
                        $"v1/billing/plans/{plan.Id}",
                        async err =>
                        {
                            var errorBody = await err.Content.ReadAsStringAsync();
                            throw new InvalidOperationException($"Error al obtener los detalles de los planes");
                        });
                    var planDetails = JsonConvert.DeserializeObject<PaypalPlanResponseDto>(requestPlanDetails);
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
            try
            {
                var productrequest = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Get,
                    $"v1/catalogs/products/{id}",
                    async err =>
                    {
                        var errorBody = await err.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al obtener la informacion para editar el producto: {err.StatusCode} - {errorBody}");
                    });

                // Crear la solicitud PATCH usando DTOs
                var patchRequest = BuildEditProductRequest(name, description);
                var pathrequest = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Patch,
                    $"v1/catalogs/products/{id}",
                    patchRequest,
                    async err =>
                    {
                        var errorBody = await err.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al  editar el producto: {err.StatusCode} - {errorBody}");
                    });
                return "Producto actualizado con éxito";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el producto con ID {ProductId}", id);
                throw new Exception("Ocurrio un error al actualizar el producto");
            }
        }

        private List<PatchOperationDto> BuildEditProductRequest(string name, string description)
        {
            return new List<PatchOperationDto>
                {
                    new PatchOperationDto
                    {
                        Operation = "replace",
                        Path = "/name",
                        Value = name
                    },
                    new PatchOperationDto
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
            _culture.SetInvariantCultureSafe();


            try
            {
                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Get,
                    $"v1/billing/plans/{planId}",
                    async err =>
                    {
                        var errorBody = await err.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al obtener el plan {err.StatusCode} - {errorBody}");
                    });
                var planDetails = JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(responseBody);
                var pricingUpdates = DeterminePricingUpdates(planDetails, trialAmount, regularAmount, currency, planId);

                if (!pricingUpdates.Any())
                {
                    throw new PayPalException("No se proporcionaron esquemas de precios válidos para actualizar el plan. Los precios enviados son idénticos a los actuales o no se proporcionaron cambios.");
                }
                var planRequest = new UpdatePricingPlanDto { PricingSchemes = pricingUpdates };
                var updatePlanPricing = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    $"v1/billing/plans/{planId}/update-pricing-schemes",
                    planRequest,
                     async err =>
                     {
                         var errorBody = await err.Content.ReadAsStringAsync();
                         var errorResponse = JsonConvert.DeserializeObject<PaypalErrorResponse>(errorBody);
                         if (errorResponse?.Name == "PRICING_SCHEME_INVALID_AMOUNT")
                         {
                             throw new PayPalException(
                                 "Uno o más precios son idénticos al valor actual. Debes proporcionar un precio diferente.");
                         }

                         if (errorResponse?.Name == "PRICING_SCHEME_UPDATE_NOT_ALLOWED")
                         {
                             throw new PayPalException(
                                 "No es posible actualizar el precio de un plan activo que ya tiene suscripciones asociadas. " +
                                 "Crea un nuevo plan o actualiza las suscripciones de forma individual.");
                         }

                     });
                var verifyPlanDetails = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Get,
                    $"v1/billing/plans/{planId}",
                    async err =>
                    {
                        var errorBody = await err.Content.ReadAsStringAsync();
                        var errorResponse = JsonConvert.DeserializeObject<PaypalErrorResponse>(errorBody);
                        throw new PayPalException($"No se pudo obtener los detalles actualizados del plan con ID {planId}: {err.StatusCode} - {errorResponse?.Message ?? "Error desconocido"} (Debug ID: {errorResponse?.DebugId})");
                    });
                var updatedPlanDetails = JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(verifyPlanDetails);
                await _repo.SavePlanPriceUpdateAsync(planId, new UpdatePricingPlanDto { PricingSchemes = pricingUpdates });

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
        #endregion

        #region Suscribirse a un plan
        public async Task<string> Subscribirse(string id, string returnUrl, string cancelUrl, string planName)
        {

            _culture.SetInvariantCultureSafe();
            var subscriptionRequest = BuildSubscriptionRequest(id, returnUrl, cancelUrl, planName);
            var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Post,
                "v1/billing/subscriptions",
                subscriptionRequest,
                async err =>
                {
                    var errBody = await err.Content.ReadAsStringAsync();
                    if (errBody != null)
                    {
                        throw new InvalidOperationException($"Error al subscribirse: {err.StatusCode} - {errBody}");
                    }
                });

            var subscriptionJson = JsonConvert.DeserializeObject<SubscriptionCreateRequestDto>(responseBody);
            if (subscriptionJson == null)
            {
                throw new ArgumentNullException("No se pudo iniciar el proceso para suscribirse");
            }
            var approvalLink = subscriptionJson?.Links?.FirstOrDefault(link => link.Rel == "approve")?.Href;
            if (string.IsNullOrEmpty(approvalLink))
            {
                throw new InvalidOperationException("No se encontró el enlace de aprobación en la respuesta de PayPal.");
            }
            return approvalLink;

        }
        private SubscriptionCreateRequestDto BuildSubscriptionRequest(string id, string returnUrl, string cancelUrl, string planName)
        {
            return new SubscriptionCreateRequestDto
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

            var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Get,
                $"v1/billing/subscriptions/{subscription_id}",
                async err =>
                {
                    var errBody = await err.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Error al obtener los detalles de la subscripcion");
                });

            var subscriptionDetails = JsonConvert.DeserializeObject<PaypalSubscriptionResponse>(responseBody);
            if (subscriptionDetails == null)
            {
                throw new InvalidOperationException("No se pudieron obtener los detalles de la suscripción: la respuesta del servidor no es válida.");
            }
            return subscriptionDetails;
        }
        #endregion

        #region desactivar plan de suscripcion
        public async Task<string> DesactivarPlan(string planId)
        {
            var deactivatePlanRequest = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Post,
                $"v1/billing/plans/{planId}/deactivate",
                async error =>
                {
                    var errBody = await error.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Error al desactivar el plan: {error.StatusCode}- {errBody}");
                });
            var planDetailsResponseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Get,
                $"v1/billing/plans/{planId}",
                async error =>
                {
                    var errBody = await error.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Error al obtener el plan: {error.StatusCode}- {errBody}");
                });

            var planDetails = JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(planDetailsResponseBody);
            if (planDetails == null)
            {
                throw new ArgumentNullException("No se puede desactivar el plan: respuesta del servidor no valida");
            }
            string planStatusResult = planDetails.Status;
            await _repo.UpdatePlanStatusInDatabase(planId, planStatusResult);
            return "Plan desactivado con éxito";
        }
        #endregion

        #region Activar plan
        public async Task<string> ActivarPlan(string planId)
        {
            var activatePlanRequest = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Post,
                $"v1/billing/plans/{planId}/activate",
                async error =>
                {
                    var errorBody = await error.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Error al activar el plan: {error.StatusCode} - {errorBody}");
                });
            var planDetailsResponseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Get,
                $"v1/billing/plans/{planId}",
                 async error =>
                 {
                     var errorBody = await error.Content.ReadAsStringAsync();
                     throw new InvalidOperationException($"Error al obtener el plan: {error.StatusCode} - {errorBody}");
                 });
            var planDetails = JsonConvert.DeserializeObject<PaypalPlanDetailsDto>(planDetailsResponseBody);
            if (planDetails == null)
            {
                throw new ArgumentNullException("No se puede activar el plan: respuesta del servidor no valida");
            }
            string planStatusResult = planDetails.Status;
            await _repo.UpdatePlanStatusInDatabase(planId, planStatusResult);
            return "Plan activado con éxito";
        }
        #endregion

        #region Cancelar Subscripcion
        public async Task<string> CancelarSuscripcion(string subscription_id, string reason)
        {
            var subscriptionRequest = new
            {
                reason = reason,
            };
            var response = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Post,
                $"v1/billing/subscriptions/{subscription_id}/cancel",
                subscriptionRequest,
               async error =>
               {
                   var errorBody = await error.Content.ReadAsStringAsync();
                   throw new InvalidOperationException($"Error al cancelar la subscripcion");
               });
            await ObtenerDetallesSuscripcion(subscription_id);
            return "Suscripción cancelada exitosamente";
        }
        #endregion

        #region Suspender suscripcion
        public async Task<string> SuspenderSuscripcion(string subscription_id, string reason)
        {

            var razon = new { reason = reason };
            var suspendSubscriptionRequest = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Post,
                $"v1/billing/subscriptions/{subscription_id}/suspend",
                razon,
                async error =>
                {
                    var errorBody = await error.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Error al suspender la subscripcion");
                });

            var planDetailsResponseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Get,
                $"v1/billing/subscriptions/{subscription_id}",
                async error =>
                {
                    var errorBody = await error.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Error al obtener la subscripcion");
                });
            var subscriptionDetails = JsonConvert.DeserializeObject<SuspendSubscription>(planDetailsResponseBody);
            if (subscriptionDetails == null)
            {
                throw new ArgumentNullException("No se puede suspender la suscripcion: respuesta del servidor no valida");
            }
            string subscriptionResult = subscriptionDetails.Status;
            return "Subscripcion suspendida con éxito";
        }
        #endregion

        #region Activar suscripcion
        public async Task<string> ActivarSuscripcion(string subscription_id, string reason)
        {
            var razon = new { reason = reason };
            var activateSubscriptionRequest = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Post,
                $"v1/billing/subscriptions/{subscription_id}/activate",
                razon,
                async error =>
                {
                    var errorBody = await error.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Error al activar la subscripcion");
                });

            var planDetailsResponseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Get,
                $"v1/billing/subscriptions/{subscription_id}",
                  async error =>
                  {
                      var errorBody = await error.Content.ReadAsStringAsync();
                      throw new InvalidOperationException($"Error al obtener la subscripcion");
                  });
            var activateSubscription = JsonConvert.DeserializeObject<ActivateSubscription>(planDetailsResponseBody);
            if (activateSubscription == null)
            {
                throw new ArgumentNullException("No se puede activar la suscripcion: respuesta del servidor no valida");
            }
            string activateResult = activateSubscription.Status;
            return "Subscripcion activada con éxito";
        }
        #endregion  
    }
}
