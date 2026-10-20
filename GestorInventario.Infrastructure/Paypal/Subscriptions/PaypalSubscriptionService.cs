using System.Globalization;
using GestorInventario.Application.DTOS;
using GestorInventario.Application.Exceptions;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Subscriptions;
using GestorInventario.Infrastructure.ExternalServices;
using GestorInventario.Infrastructure.Mappers;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.Paypal.Subscriptions
{
    public class PaypalSubscriptionService : IPaypalSubscriptionService
    {
        private readonly ILogger<PaypalSubscriptionService> _logger;
        private readonly IPayPalHttpClient _paypal;


        public PaypalSubscriptionService(ILogger<PaypalSubscriptionService> logger,
            IPayPalHttpClient paypal)
        {
            _logger = logger;
            _paypal = paypal;
        }

        #region creacion de un producto y plan de suscripcion

        public async Task<OperationResult<string>> CreateProductAsync(string productName, string productDescription,
            string productType, string productCategory)
        {
            try
            {
                var productRequest = BuildProductRequest(productName, productDescription, productType, productCategory);
                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    $"v1/catalogs/products/",
                    productRequest,
                    async error =>
                    {
                        var errorBody = await error.Content.ReadAsStringAsync();
                        throw new InvalidOperationException(
                            $"Error al crear el producto {error.StatusCode} - {errorBody}");
                    });
                var productResponse = JsonConvert.DeserializeObject<CreateProductResponse>(responseBody);

                string productId = productResponse.Id;
                return OperationResult<string>.Ok(productId, "Producto creado con exito");
            }
            catch (InvalidOperationException ex)
            {
                // Capturamos los errores de PayPal o los errores de Unwrap()
                return OperationResult<string>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        // Update the BuildProductRequest to use the request DTO
        private CreateProductRequest BuildProductRequest(string productName, string productDescription,
            string productType, string productCategory)
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

        public async Task<OperationResult<string>> CreateSubscriptionPlanAsync(string productId, string planName,
            string description, decimal amount, string currency, string intervalUnit, int trialDays = 0,
            decimal trialAmount = 0.00m)
        {
            CultureHelper.SetInvariantCulture();
            try
            {
                var planRequest = BuildPaypalPlanRequest(productId, planName, description, amount, currency, trialDays,
                    trialAmount, intervalUnit);

                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    "v1/billing/plans",
                    planRequest,
                    async err =>
                    {
                        var errorBody = await err.Content.ReadAsStringAsync();
                        throw new InvalidOperationException(
                            $"Error al crear la suscripcion {err.StatusCode} - {errorBody}");
                    });

                var responseObject = JsonConvert.DeserializeObject<PaypalPlanDetailsResponse>(responseBody);

                string createdPlanId = responseObject.Id;
                return OperationResult<string>.Ok(createdPlanId, "Plano creado con exito");
            }
            catch (InvalidOperationException ex)
            {
                // Capturamos los errores de PayPal o los errores de Unwrap()
                return OperationResult<string>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el plan de suscripción");
                return OperationResult<string>.Fail(
                    "Ocurrio un error al crear el plan de suscripcion intentelo de nuevo mas tarde");
            }
        }

        private PlanRequest BuildPaypalPlanRequest(
            string productId,
            string planName,
            string description,
            decimal amount,
            string currency,
            int trialDays = 0,
            decimal trialAmount = 0.00m,
            string intervalUnit = "MONTH")
        {
            var billingCycles = new List<BillingCycleRq>();

            // Ciclo de prueba (si existe)
            if (trialDays > 0)
            {
                billingCycles.Add(new BillingCycleRq
                {
                    TenureType = "TRIAL",
                    Sequence = 1,
                    Frequency = new FrequencyRq
                    {
                        IntervalUnit = "DAY",
                        IntervalCount = trialDays
                    },
                    TotalCycles = 1,
                    PricingScheme = new PricingSchemeRq
                    {
                        FixedPrice = new FixedPriceRq
                        {
                            Value = trialAmount.ToString("0.00", CultureInfo.InvariantCulture),
                            CurrencyCode = currency
                        }
                    }
                });
            }


            billingCycles.Add(new BillingCycleRq
            {
                TenureType = "REGULAR",
                Sequence = trialDays > 0 ? 2 : 1,
                Frequency = new FrequencyRq
                {
                    IntervalUnit = intervalUnit, // "MONTH" o "YEAR"
                    IntervalCount = 1
                },
                TotalCycles = intervalUnit == "YEAR" ? 0 : 12, // ← Aquí está el cambio importante
                PricingScheme = new PricingSchemeRq
                {
                    FixedPrice = new FixedPriceRq
                    {
                        Value = amount.ToString("0.00", CultureInfo.InvariantCulture),
                        CurrencyCode = currency
                    }
                }
            });

            return new PlanRequest
            {
                ProductId = productId,
                Name = planName,
                Description = description,
                Status = "ACTIVE",
                BillingCycles = billingCycles.ToArray(),
                PaymentPreferences = new PaymentPreferencesRq
                {
                    AutoBillOutstanding = true,
                    SetupFee = new FixedPriceRq { Value = "0.00", CurrencyCode = currency },
                    SetupFeeFailureAction = "CONTINUE",
                    PaymentFailureThreshold = 3
                },
                Taxes = new TaxesRq
                {
                    Percentage = "10",
                    Inclusive = false
                }
            };
        }

        #endregion

        #region Obtener detalles del plan

        public async Task<OperationResult<PlanDetailsDto>> ObtenerDetallesPlan(string id)
        {
            if (string.IsNullOrEmpty(id))
                return OperationResult<PlanDetailsDto>.Fail("El ID del plan es requerido.");

            try
            {
                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Get,
                    $"v1/billing/plans/{id}",
                    async error =>
                    {
                        var errBody = await error.Content.ReadAsStringAsync();
                        throw new InvalidOperationException(
                            $"Error al obtener los detalles del plan: {error.StatusCode} - {errBody}");
                    });

                var planDetails = JsonConvert.DeserializeObject<PaypalPlanDetailsResponse>(responseBody);
                if (planDetails == null)
                    return OperationResult<PlanDetailsDto>.Fail("No se pudieron obtener los detalles del plan.");

                return OperationResult<PlanDetailsDto>.Ok(PlanDetailsMapper.ToDto(planDetails));
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult<PlanDetailsDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener los detalles del plan {PlanId}", id);
                return OperationResult<PlanDetailsDto>.Fail($"Error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region Obtener producto asociado a un plan

        public async Task<OperationResult<ProductsPageDto>> GetProductsAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Get,
                    $"v1/catalogs/products?page_size={pageSize}&page={page}",
                    async err =>
                    {
                        var errorBody = await err.Content.ReadAsStringAsync();
                        throw new InvalidOperationException(
                            $"Error al obtener los productos {err.StatusCode} - {errorBody}");
                    });

                var jsonResponse = JsonConvert.DeserializeObject<PaypalProductCollectionResponse>(responseBody);
                if (jsonResponse == null)
                    return OperationResult<ProductsPageDto>.Fail("Respuesta de productos vacía");

                var hasNextPage = jsonResponse.Links?.Any(link => link.Rel == "next") ?? false;

                var dto = new ProductsPageDto
                {
                    HasNextPage = hasNextPage,
                    Products = jsonResponse.Products?
                        .Select(p => new ProductSummaryDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Description = p.Description
                        })
                        .ToList() ?? new List<ProductSummaryDto>()
                };

                return OperationResult<ProductsPageDto>.Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult<ProductsPageDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult<ProductsPageDto>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        #endregion
/*
 * public async Task<OperationResult<ProductsPageDto>> GetProductsAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Get,
                    $"v1/catalogs/products?page_size={pageSize}&page={page}",
                    async err =>
                    {
                        var errorBody = await err.Content.ReadAsStringAsync();
                        throw new InvalidOperationException(
                            $"Error al obtener los productos {err.StatusCode} - {errorBody}");
                    });

                var jsonResponse = JsonConvert.DeserializeObject<PaypalProductCollectionResponse>(responseBody);
                if (jsonResponse == null)
                    return OperationResult<ProductsPageDto>.Fail("Respuesta de productos vacía");

                var hasNextPage = jsonResponse.Links?.Any(link => link.Rel == "next") ?? false;

                var dto = new ProductsPageDto
                {
                    HasNextPage = hasNextPage,
                    Products = jsonResponse.Products?
                        .Select(p => new ProductSummaryDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Description = p.Description
                        })
                        .ToList() ?? new List<ProductSummaryDto>()
                };

                return OperationResult<ProductsPageDto>.Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult<ProductsPageDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult<ProductsPageDto>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }
 *
 * 
 */
        #region Obtener Planes de suscripcion

        public async Task<OperationResult<SubscriptionPlansPageDto>> GetSubscriptionPlansAsync(
            int page = 1, int pageSize = 6)
        {
            try
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
                var plansListResponse = JsonConvert.DeserializeObject<PaypalPlansCollectionResponse>(responseBody);
                if (plansListResponse == null)
                {
                    throw new InvalidOperationException("No se pudo obtener los planes");
                }

                bool hasNextPage = plansListResponse.Links.Any(link => link.Rel == "next");
                var detailedPlans = new List<PaypalPlanResponse>();
                // Obtener detalles completos de cada plan (si quieres detalles completos)
                foreach (var plan in plansListResponse.Plans)
                {
                    var requestPlanDetails = await _paypal.ExecutePayPalRequestAsync<string>(
                        HttpMethod.Get,
                        $"v1/billing/plans/{plan.Id}",
                        async err =>
                        {
                            var errorBody = await err.Content.ReadAsStringAsync();
                            throw new InvalidOperationException($"Error al obtener los detalles de los planes");
                        });
                    var planDetails = JsonConvert.DeserializeObject<PaypalPlanResponse>(requestPlanDetails);

                    if (planDetails != null)
                        detailedPlans.Add(planDetails);
                }
                var result = PlanPageMapper.ToPageDto(detailedPlans, hasNextPage);
                return OperationResult<SubscriptionPlansPageDto>.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // Capturamos los errores de PayPal o los errores de Unwrap()
                return OperationResult<SubscriptionPlansPageDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar detalles del plan");
                return OperationResult<SubscriptionPlansPageDto>.Fail("Error al obtener los detalles del plan");
            }
        }

        #endregion

        #region Editar producto vinculado a un plan

        public async Task<OperationResult<string>> EditarProducto(string id, string name, string description)
        {
            try
            {
                await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Get,
                    $"v1/catalogs/products/{id}",
                    async err =>
                    {
                        var errorBody = await err.Content.ReadAsStringAsync();
                        throw new InvalidOperationException(
                            $"Error al obtener la informacion para editar el producto: {err.StatusCode} - {errorBody}");
                    });

                // Crear la solicitud PATCH usando DTOs
                var patchRequest = BuildEditProductRequest(name, description);
                await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Patch,
                    $"v1/catalogs/products/{id}",
                    patchRequest,
                    async err =>
                    {
                        var errorBody = await err.Content.ReadAsStringAsync();
                        throw new InvalidOperationException(
                            $"Error al  editar el producto: {err.StatusCode} - {errorBody}");
                    });
                return OperationResult<string>.Ok("Producto actualizado con éxito");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el producto con ID {ProductId}", id);
                throw new Exception("Ocurrio un error al actualizar el producto");
            }
        }

        private List<PatchOperationRequest> BuildEditProductRequest(string name, string description)
        {
            return new List<PatchOperationRequest>
            {
                new PatchOperationRequest
                {
                    Operation = "replace",
                    Path = "/name",
                    Value = name
                },
                new PatchOperationRequest
                {
                    Operation = "replace",
                    Path = "/description",
                    Value = description
                }
            };
        }

        #endregion

        #region Actualizar precios de un plan

        public async Task<OperationResult<PlanPriceUpdateResultDto>> UpdatePricingPlanAsync(
            string planId,
            decimal? trialAmount,
            decimal regularAmount,
            string currency)
        {
            CultureHelper.SetInvariantCulture();
            try
            {
                var responseBody = await ObtenerDetallesPlan(planId);
                if (!responseBody.Success)
                    return OperationResult<PlanPriceUpdateResultDto>.Fail(
                        "Ocurrió un error al obtener el plan. Inténtelo de nuevo más tarde.");

                // responseBody.Data es PlanDetailsDto
                var pricingUpdates = DeterminePricingUpdates(
                    responseBody.Data, trialAmount, regularAmount, currency, planId);

                if (!pricingUpdates.Any())
                    return OperationResult<PlanPriceUpdateResultDto>.Fail(
                        "No se proporcionaron esquemas de precios válidos para actualizar el plan. " +
                        "Los precios enviados son idénticos a los actuales o no se proporcionaron cambios.");

                // Request solo de Infrastructure (PayPal)
                var planRequest = new UpdatePricingPlanRequest { PricingSchemes = pricingUpdates };

                await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    $"v1/billing/plans/{planId}/update-pricing-schemes",
                    planRequest,
                    async err =>
                    {
                        var errorBody = await err.Content.ReadAsStringAsync();
                        var errorResponse = JsonConvert.DeserializeObject<PaypalErrorResponse>(errorBody);

                        if (errorResponse?.Name == "PRICING_SCHEME_INVALID_AMOUNT")
                            throw new InvalidOperationException(
                                "Uno o más precios son idénticos al valor actual. Debes proporcionar un precio diferente.");

                        if (errorResponse?.Name == "PRICING_SCHEME_UPDATE_NOT_ALLOWED")
                            throw new InvalidOperationException(
                                "No es posible actualizar el precio de un plan activo que ya tiene suscripciones asociadas. " +
                                "Crea un nuevo plan o actualiza las suscripciones de forma individual.");

                        throw new InvalidOperationException(
                            $"Error al actualizar precios: {err.StatusCode} - {errorBody}");
                    });

                // DTO limpio hacia Application
                var resultDto = new PlanPriceUpdateResultDto
                {
                    PlanId = planId,
                    PricingSchemes = pricingUpdates.Select(u => new PlanPriceSchemeDto
                    {
                        BillingCycleSequence = u.BillingCycleSequence,
                        Value = u.PricingScheme!.FixedPrice.Value!,
                        CurrencyCode = u.PricingScheme.FixedPrice.CurrencyCode
                    }).ToList()
                };

                return OperationResult<PlanPriceUpdateResultDto>.Ok(resultDto, "Precio actualizado en PayPal");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error al actualizar el precio del plan");
                return OperationResult<PlanPriceUpdateResultDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al actualizar el precio del plan");
                return OperationResult<PlanPriceUpdateResultDto>.Fail(
                    $"Error inesperado al actualizar el precio del plan: {ex.Message}");
            }
        }

        private List<UpdatePricingSchemes> DeterminePricingUpdates(
            PlanDetailsDto planDetails,
            decimal? trialAmount,
            decimal regularAmount,
            string currency,
            string planId)
        {
            var pricingSchemes = new List<UpdatePricingSchemes>();
            var (hasTrial, currentTrialPrice, currentRegularPrice) = AnalyzeCurrentPricing(planDetails);

            HandleTrialPricingUpdate(pricingSchemes, hasTrial, trialAmount, currentTrialPrice, currency, planId);
            HandleRegularPricingUpdate(pricingSchemes, hasTrial, regularAmount, currentRegularPrice, currency, planId);

            return pricingSchemes;
        }

        private (bool hasTrial, decimal? trialPrice, decimal? regularPrice)
            AnalyzeCurrentPricing(PlanDetailsDto planDetails)
        {
            bool hasTrial = planDetails.BillingCycles.Any(bc => bc.TenureType == "TRIAL");
            decimal? currentTrialPrice = null;
            decimal? currentRegularPrice = null;

            foreach (var cycle in planDetails.BillingCycles)
            {
                if (cycle.TenureType == "TRIAL" && cycle.PricingScheme?.FixedPrice?.Value != null)
                    currentTrialPrice = decimal.Parse(
                        cycle.PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture);
                else if (cycle.TenureType == "REGULAR" && cycle.PricingScheme?.FixedPrice?.Value != null)
                    currentRegularPrice = decimal.Parse(
                        cycle.PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture);
            }

            return (hasTrial, currentTrialPrice, currentRegularPrice);
        }

        private void HandleTrialPricingUpdate(List<UpdatePricingSchemes> pricingSchemes, bool hasTrial,
            decimal? trialAmount, decimal? currentTrialPrice, string currency, string planId)
        {
            if (!hasTrial) return;

            if (trialAmount.HasValue && trialAmount != currentTrialPrice)
            {
                pricingSchemes.Add(CreatePricingScheme(1, trialAmount.Value, currency));
                _logger.LogInformation($"Incluyendo ciclo de prueba para el plan {planId} con precio {trialAmount}.");
            }
            else if (trialAmount.HasValue && trialAmount == currentTrialPrice)
            {
                _logger.LogInformation(
                    $"El precio de prueba {trialAmount} es idéntico al actual ({currentTrialPrice}) para el plan {planId}. Se omite el ciclo de prueba.");
            }
            else if (!trialAmount.HasValue)
            {
                _logger.LogInformation(
                    $"No se proporcionó un precio de prueba para el plan {planId}. Se omite el ciclo de prueba.");
            }
        }

        private void HandleRegularPricingUpdate(List<UpdatePricingSchemes> pricingSchemes, bool hasTrial,
            decimal regularAmount, decimal? currentRegularPrice, string currency, string planId)
        {
            if (regularAmount != currentRegularPrice)
            {
                pricingSchemes.Add(CreatePricingScheme(hasTrial ? 2 : 1, regularAmount, currency));
                _logger.LogInformation($"Incluyendo ciclo regular para el plan {planId} con precio {regularAmount}.");
            }
            else
            {
                _logger.LogInformation(
                    $"El precio regular {regularAmount} es idéntico al actual ({currentRegularPrice}) para el plan {planId}. Se omite el ciclo regular.");
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

        public async Task<OperationResult<string>> Subscribirse(string id, string returnUrl, string cancelUrl,
            string planName)
        {
            try
            {
                CultureHelper.SetInvariantCulture();
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

                var subscriptionJson = JsonConvert.DeserializeObject<SubcriptionCreateResponse>(responseBody);
                if (subscriptionJson == null)
                {
                    throw new InvalidOperationException("No se pudo iniciar el proceso para suscribirse");
                }

                var approvalLink = subscriptionJson?.Links?.FirstOrDefault(link => link.Rel == "approve")?.Href;
                if (string.IsNullOrEmpty(approvalLink))
                {
                    throw new InvalidOperationException(
                        "No se encontró el enlace de aprobación en la respuesta de PayPal.");
                }

                return OperationResult<string>.Ok(approvalLink, "Redirigiendo...");
            }
            catch (InvalidOperationException ex)
            {
                // Capturamos los errores de PayPal o los errores de Unwrap()
                return OperationResult<string>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                // Capturamos cualquier otro error inesperado (red, timeout, etc.)
                return OperationResult<string>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        private SubscriptionCreateRequest BuildSubscriptionRequest(string id, string returnUrl, string cancelUrl,
            string planName)
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

        public async Task<OperationResult<PaypalSubscriptionDto>> ObtenerDetallesSuscripcion(string subscriptionId)
        {
            try
            {
                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Get,
                    $"v1/billing/subscriptions/{subscriptionId}",
                    async err =>
                    {
                        var errBody = await err.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al obtener los detalles de la subscripcion");
                    });

                var subscriptionDetails = JsonConvert.DeserializeObject<PaypalSubscriptionResponse>(responseBody);
                var result = SubscriptionDetailsMapper.ToDto(subscriptionDetails);
                return OperationResult<PaypalSubscriptionDto>.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // Capturamos los errores de PayPal o los errores de Unwrap()
                return OperationResult<PaypalSubscriptionDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                // Capturamos cualquier otro error inesperado (red, timeout, etc.)
                return OperationResult<PaypalSubscriptionDto>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region desactivar plan de suscripcion

        public async Task<OperationResult<(string planId, string planStatus)>> DesactivarPlan(string planId)
        {
            try
            {
                await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    $"v1/billing/plans/{planId}/deactivate",
                    async error =>
                    {
                        var errBody = await error.Content.ReadAsStringAsync();
                        throw new InvalidOperationException(
                            $"Error al desactivar el plan: {error.StatusCode}- {errBody}");
                    });
                var planresponse = await ObtenerDetallesPlan(planId);
                var plan = planresponse.Unwrap();
                var planStatusResult = plan.Status;

                return OperationResult<(string, string)>.Ok((planId, planStatusResult), "Plan desactivado con exito");
            }
            catch (InvalidOperationException ex)
            {
                // Capturamos los errores de PayPal o los errores de Unwrap()
                return OperationResult<(string, string)>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                // Capturamos cualquier otro error inesperado (red, timeout, etc.)
                return OperationResult<(string, string)>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region Activar plan

        public async Task<OperationResult<(string planId, string planStatus)>> ActivarPlan(string planId)
        {
            try
            {
                await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    $"v1/billing/plans/{planId}/activate",
                    async error =>
                    {
                        var errorBody = await error.Content.ReadAsStringAsync();
                        throw new InvalidOperationException(
                            $"Error al activar el plan: {error.StatusCode} - {errorBody}");
                    });
                var planDetails = await ObtenerDetallesPlan(planId);
                var detalles = planDetails.Unwrap();
                string planStatusResult = detalles.Status;

                return OperationResult<(string, string)>.Ok((planId, planStatusResult), "Plan activado con éxito");
            }
            catch (InvalidOperationException ex)
            {
                // Capturamos los errores de PayPal o los errores de Unwrap()
                return OperationResult<(string, string)>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                // Capturamos cualquier otro error inesperado (red, timeout, etc.)
                return OperationResult<(string, string)>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region Cancelar Subscripcion

        public async Task<OperationResult<(string subId, string subStatus)>> CancelarSuscripcion(string subscriptionId,
            string reason)
        {
            try
            {
                var subscriptionRequest = new
                {
                    reason = reason,
                };
                await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    $"v1/billing/subscriptions/{subscriptionId}/cancel",
                    subscriptionRequest,
                    async error =>
                    {
                        var errorBody = await error.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al cancelar la subscripcion");
                    });
                var detallesSubscripcion = await ObtenerDetallesSuscripcion(subscriptionId);
                var detalles = detallesSubscripcion.Unwrap();
                string status = detalles.Status;
                return OperationResult<(string, string)>.Ok((subscriptionId, status),
                    "Suscripción cancelada exitosamente");
            }
            catch (InvalidOperationException ex)
            {
                // Capturamos los errores de PayPal o los errores de Unwrap()
                return OperationResult<(string, string)>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                // Capturamos cualquier otro error inesperado (red, timeout, etc.)
                return OperationResult<(string, string)>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region Suspender suscripcion

        public async Task<OperationResult<(string subId, string subStatus)>> SuspenderSuscripcion(string subscriptionId,
            string reason)
        {
            try
            {
                var razon = new { reason = reason };
                await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    $"v1/billing/subscriptions/{subscriptionId}/suspend",
                    razon,
                    async error =>
                    {
                        var errorBody = await error.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al suspender la subscripcion");
                    });

                var subscriptionDetails = await ObtenerDetallesSuscripcion(subscriptionId);
                var details = subscriptionDetails.Unwrap();
                string subscriptionStatus = details.Status;
                return OperationResult<(string, string)>.Ok((subscriptionId, subscriptionStatus),
                    "Subscripcion suspendida con éxito");
            }
            catch (InvalidOperationException ex)
            {
                // Capturamos los errores de PayPal o los errores de Unwrap()
                return OperationResult<(string, string)>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                // Capturamos cualquier otro error inesperado (red, timeout, etc.)
                return OperationResult<(string, string)>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        #endregion

        #region Activar suscripcion

        public async Task<OperationResult<(string subId, string subStatus)>> ActivarSuscripcion(string subscriptionId,
            string reason)
        {
            try
            {
                var razon = new { reason = reason };

                // 1. Ejecutar la petición de activación
                await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    $"v1/billing/subscriptions/{subscriptionId}/activate",
                    razon,
                    async error =>
                    {
                        var errorBody = await error.Content.ReadAsStringAsync();
                        // Aquí podrías loguear errorBody para saber EXACTAMENTE por qué falló PayPal
                        _logger.LogError($"Error de PayPal al activar la suscripción: {errorBody}");
                    });

                // 2. Obtener los detalles para confirmar el estado actual
                var subscriptionDetailsResult = await ObtenerDetallesSuscripcion(subscriptionId);

                // 3. 
                // Si ObtenerDetallesSuscripcion falló o el Data es null, Unwrap lanzará una excepción
                // que será capturada por el catch, devolviendo un Fail ordenado.
                var details = subscriptionDetailsResult.Unwrap();

                string subscriptionStatus = details.Status;

                return OperationResult<(string, string)>.Ok(
                    (subscriptionId, subscriptionStatus), "Suscripción activada con éxito"
                );
            }
            catch (InvalidOperationException ex)
            {
                // Capturamos los errores de PayPal o los errores de Unwrap()
                return OperationResult<(string, string)>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                // Capturamos cualquier otro error inesperado (red, timeout, etc.)
                return OperationResult<(string, string)>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
        }

        #endregion
    }
}