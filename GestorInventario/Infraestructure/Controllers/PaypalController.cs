using AutoMapper;
using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels.Paypal.GestorInventario.Domain.Models.ViewModels.Paypal;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.ViewModels.product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class PaypalController : Controller
    {
        private readonly GestorInventarioContext _context;
       
        private readonly GenerarPaginas _generarPaginas;
     
        private readonly ILogger<PaypalController> _logger;
        private readonly IPaypalRepository _paypalRepository;
        private readonly ICarritoRepository _carritoRepository;
        private readonly IMapper _mapper;
        private readonly PolicyExecutor _policyExecutor;
        private readonly IPaypalService _paypalService;
        public PaypalController(GestorInventarioContext context, GenerarPaginas generar,  ILogger<PaypalController> logger, 
            IPaypalRepository paypalController, ICarritoRepository carritoRepository, IMapper map, PolicyExecutor executor, IPaypalService service)
        {
            _context = context;
            _paypalService= service;
            _generarPaginas = generar;
         _policyExecutor=executor;
            _logger = logger;
            _paypalRepository = paypalController;
            _carritoRepository = carritoRepository;
            _mapper = map;
        }

        //Metodo que muestra los productos creados en paypal
        [HttpGet]
        public async Task<IActionResult> MostrarProductos(int pagina = 1)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                int cantidadAMostrar = 6;

                // Obtener productos de PayPal
                var (respuestaProductos, tienePaginaSiguiente) = await _policyExecutor.ExecutePolicyAsync(() =>
                _paypalService.GetProductsAsync(pagina, cantidadAMostrar));

                // Deserializar la respuesta JSON
                var productosJson = JsonConvert.DeserializeObject<PaypalProductResponse>(respuestaProductos);

                // Mapear productos a ProductoPaypalViewModel
                var productos = productosJson?.Products?.Select(p => new ProductoPaypalViewModel
                {
                    Id = p.Id,
                    Nombre = p.Name,
                    Descripcion = p.Description
                }).ToList() ?? new List<ProductoPaypalViewModel>();

                // Calcular total de páginas (PayPal no proporciona totalItems, usamos tienePaginaSiguiente)
                // Nota: Esto es una aproximación, ya que PayPal no proporciona el conteo total de productos.
                var totalPaginas = tienePaginaSiguiente ? pagina + 1 : pagina;

                // Configurar paginación
                var paginacion = new Paginacion
                {
                    Pagina = pagina,
                    CantidadAMostrar = cantidadAMostrar,
                    TotalPaginas = totalPaginas,
                    Radio = 3
                };
                var paginas = _generarPaginas.GenerarListaPaginas(paginacion);

                // Crear el ViewModel
                var model = new ProductosPaypalViewModel
                {
                    Productos = productos,
                    Paginas = paginas,
                    TotalPaginas = totalPaginas,
                    PaginaActual = pagina,
                    TienePaginaSiguiente = tienePaginaSiguiente,
                    TienePaginaAnterior = pagina > 1
                };

                return View(model);
            }
            catch (Exception ex)
            {
               
                _logger.LogError(ex, "Error al obtener los productos de PayPal");
                return RedirectToAction("Error", "Home");
            }
        }


        [HttpGet]
        public async Task<IActionResult> MostrarPlanes([FromQuery] int pagina = 1, [FromQuery] int cantidadAMostrar = 6)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                _logger.LogInformation("Página solicitada: {Pagina}, CantidadAMostrar: {Cantidad}", pagina, cantidadAMostrar);

                // Obtener planes de suscripción de PayPal
                var (plans, hasNextPage) = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalService.GetSubscriptionPlansAsync(pagina, cantidadAMostrar));

                // Mapear los planes a PlanesViewModel
                var planesViewModel = _mapper.Map<List<PlanesViewModel>>(plans ?? new List<Plan>());

                // Calcular TotalPaginas basado en hasNextPage (aproximación)
                int totalPaginas = hasNextPage ? pagina + 1 : pagina;

                // Configurar paginación
                var paginacion = new Paginacion
                {
                    Pagina = pagina,
                    CantidadAMostrar = cantidadAMostrar,
                    TotalPaginas = totalPaginas,
                    Radio = 3
                };
                var paginas = _generarPaginas.GenerarListaPaginas(paginacion);

                // Crear el modelo para la vista
                var model = new PlanesPaginadosViewModel
                {
                    Planes = planesViewModel,
                    Paginas = paginas,
                    TotalPaginas = totalPaginas,
                    PaginaActual = pagina,
                    TienePaginaSiguiente = hasNextPage,
                    TienePaginaAnterior = pagina > 1,
                    CantidadAMostrar = cantidadAMostrar
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "Error al obtener los planes de suscripción. Intenta de nuevo más tarde.";
                _logger.LogError(ex, "Error al obtener los planes de suscripción para la página {Pagina}", pagina);
                return RedirectToAction("Error", "Home");
            }
        }
    
        // Método para crear la lista de categorías a partir de la enumeración
        private List<string> GetCategoriesFromEnum()
        {
            return Enum.GetNames(typeof(Category)).ToList();
        }
        //Metodo que obtiene los datos necesarios antes de crear el producto al que se suscribira
        public async Task<IActionResult> CrearProducto()
        {
          
            var model = new ProductViewModelPaypal();
            ViewData["Moneda"] = new SelectList(await _policyExecutor.ExecutePolicyAsync(() => _carritoRepository.ObtenerMoneda()), "Codigo", "Codigo");
            // Obtener las categorías desde la enumeración y asignarlas al modelo
            model.Categories = GetCategoriesFromEnum();

            return View(model);
        }

      //Metodo que crea el producto al que se suscribira el usuario
        [HttpPost]
        public async Task<IActionResult> CrearProducto(ProductViewModelPaypal model, string monedaSeleccionada)
        {
            // Cambia la cultura actual del hilo a InvariantCulture
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            if (ModelState.IsValid)
            {
                try
                {
                    // Crear un producto usando los datos del formulario
                    var productResponse = await _paypalService.CreateProductAndNotifyAsync(model.Name, model.Description, model.Type, model.Category);
                    dynamic productJson = JsonConvert.DeserializeObject(productResponse);
                    string productId = productJson.id;
                    ViewData["Moneda"] = new SelectList(await _carritoRepository.ObtenerMoneda(), "Codigo", "Codigo");
                    // Crear el plan de suscripción
                    string planResponse = await _paypalService.CreateSubscriptionPlanAsync(
                        productId,
                        model.PlanName,
                        model.PlanDescription,
                        model.Amount,
                        monedaSeleccionada,
                        model.HasTrialPeriod ? model.TrialPeriodDays : 0, // Pasamos los días de prueba
                        model.HasTrialPeriod ? model.TrialAmount : 0.00m   // Pasamos el costo del periodo de prueba
                    );


                    if (planResponse.Contains("\"error\""))
                    {
                        return StatusCode(500, $"Error al crear el plan de suscripción: {planResponse}");
                    }

                    return RedirectToAction(nameof(MostrarProductos));
                }
                catch (Exception ex)
                {

                    return StatusCode(500, $"Error inesperado: {ex.Message}");
                }
            }


            model.Categories = GetCategoriesFromEnum();
            return View(model);
        }

        //Metodo para desactivar el plan
        [HttpPost]
        public async Task<IActionResult> DesactivarPlan(string productId, string planId)
        {
            try
            {
                var activeSubscriptions = await _paypalRepository.ObtenerSuscriptcionesActivas(planId);
                var userSubscriptions = await _paypalRepository.SusbcripcionesUsuario(planId);
                if (activeSubscriptions.Any() || userSubscriptions.Any())
                {
                    return StatusCode(400, "No se puede cancelar el plan porque hay suscriptores activos.");
                }
                var deleteResponse = await _paypalService.DesactivarPlan(productId, planId);


                return RedirectToAction(nameof(MostrarPlanes), new { mensaje = deleteResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar el producto y el plan: {ex.Message}");
            }
        }

        //Metodo para desactivar el producto
        [HttpPost]
        public async Task<IActionResult> DesactivarProducto(string id)
        {
            try
            {

                var deleteResponse = await _paypalService.MarcarDesactivadoProducto(id);


                return RedirectToAction(nameof(MostrarProductos), new { mensaje = deleteResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar el producto y el plan: {ex.Message}");
            }
        }
        //Metodo que muestra la vista para editar
        public IActionResult EditarProductoPaypal()
        {
            return View();
        }
        //Metodo que edita el producto de paypal
        [HttpPost]
        public async Task<IActionResult> EditarProductoPaypal(string id, EditProductPaypal model)
        {

            if (ModelState.IsValid)
            {

                try
                {
                    var authToken = await _paypalService.GetAccessTokenAsync();
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                        var productResponse1 = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/catalogs/products/{id}");
                        if (!productResponse1.IsSuccessStatusCode)
                        {
                            var errorContent = await productResponse1.Content.ReadAsStringAsync();
                            throw new Exception($"No se pudo encontrar el producto con ID {id}: {productResponse1.StatusCode} - {errorContent}");
                        }
                    }

                    var productResponse = await _paypalService.EditarProducto(id, model.name, model.description);
                    return RedirectToAction(nameof(MostrarProductos));
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error al crear el plan de suscripción: {ex.Message}");
                }
            }

            return View(model);
        }
        //Metodo que inicia el proceso de suscripcion
        [HttpPost]
        public async Task<IActionResult> IniciarSuscripcion( string plan_id, string planName)
        {
            // Define las URLs de retorno y cancelación
            string returnUrl = Url.Action("ConfirmarSuscripcion", "Paypal", null, Request.Scheme);
            string cancelUrl = Url.Action("CancelarSuscripcion", "Paypal", null, Request.Scheme);      
            try
            {
                
                string approvalUrl = await _paypalService.Subscribirse(plan_id, returnUrl, cancelUrl, planName);

             
                return Redirect(approvalUrl);
            }
            catch (Exception ex)
            {
           
                TempData["ErrorMessage"] = $"Error al iniciar la suscripción: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }


      
        public async Task<IActionResult> ConfirmarSuscripcion(string subscription_id, string token, string ba_token)
        {
            try
            {
                if (string.IsNullOrEmpty(subscription_id) || string.IsNullOrEmpty(token))
                {
                    TempData["ErrorMessage"] = "No se pudo confirmar la suscripción. Faltan parámetros requeridos.";
                    return RedirectToAction("Error", "Home");
                }

                var existeUsuario = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(existeUsuario, out int usuarioId))
                {
                    TempData["ErrorMessage"] = "No se pudo identificar al usuario.";
                    return RedirectToAction("Error", "Home");
                }

                var subscriptionDetails = await _policyExecutor.ExecutePolicyAsync(() => _paypalService.ObtenerDetallesSuscripcion(subscription_id));
                if (subscriptionDetails == null)
                {
                    TempData["ErrorMessage"] = "No se pudieron obtener los detalles de la suscripción desde PayPal.";
                    return RedirectToAction("Error", "Home");
                }

                string planId = subscriptionDetails.plan_id ?? string.Empty;
                var plan = await _policyExecutor.ExecutePolicyAsync(() => _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId));
                if (plan == null)
                {
                    // Obtener detalles del plan desde PayPal y mapear a PaypalPlanDetailsDto
                    var planResponse = await _policyExecutor.ExecutePolicyAsync(() => _paypalService.ObtenerDetallesPlan(planId));
                    var planDetails = new PaypalPlanDetailsDto
                    {
                        Id = planResponse.Id,
                        ProductId = planResponse.ProductId,
                        Name = planResponse.Name,
                        Description = planResponse.Description,
                        Status = planResponse.Status,
                        PaymentPreferences = new PaymentPreferencesDto
                        {
                            AutoBillOutstanding = planResponse.PaymentPreferences.AutoBillOutstanding,
                            SetupFee = planResponse.PaymentPreferences.SetupFee != null
                                ? new FixedPriceDto
                                {
                                    Value = planResponse.PaymentPreferences.SetupFee.Value.ToString(CultureInfo.InvariantCulture),
                                    CurrencyCode = planResponse.PaymentPreferences.SetupFee.CurrencyCode
                                }
                                : null,
                            SetupFeeFailureAction = planResponse.PaymentPreferences.SetupFeeFailureAction,
                            PaymentFailureThreshold = planResponse.PaymentPreferences.PaymentFailureThreshold
                        },
                        Taxes = planResponse.Taxes != null
                            ? new TaxesDto
                            {
                                Percentage = planResponse.Taxes.Percentage.ToString(CultureInfo.InvariantCulture),
                                Inclusive = planResponse.Taxes.Inclusive
                            }
                            : null,
                        BillingCycles = planResponse.BillingCycles.Select(b => new BillingCycleDto
                        {
                            TenureType = b.TenureType,
                            Sequence = b.Sequence,
                            Frequency = new FrequencyDto
                            {
                                IntervalUnit = b.Frequency.IntervalUnit,
                                IntervalCount = b.Frequency.IntervalCount
                            },
                            TotalCycles = b.TotalCycles,
                            PricingScheme = new PricingSchemeDto
                            {
                                FixedPrice = b.PricingScheme.FixedPrice != null
                                    ? new FixedPriceDto
                                    {
                                        Value = b.PricingScheme.FixedPrice.Value,
                                        CurrencyCode = b.PricingScheme.FixedPrice.CurrencyCode
                                    }
                                    : null
                            }
                        }).ToArray()
                    };

                    await _policyExecutor.ExecutePolicy(() => _paypalRepository.SavePlanDetailsAsync(planId, planDetails));
                    plan = await _policyExecutor.ExecutePolicyAsync(() => _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId));
                }

                var minSqlDate = new DateTime(1753, 1, 1);

                var detallesSuscripcion = new SubscriptionDetail
                {
                    SubscriptionId = subscriptionDetails.id ?? string.Empty,
                    PlanId = subscriptionDetails.plan_id ?? string.Empty,
                    Status = subscriptionDetails.status ?? string.Empty,
                    StartTime = subscriptionDetails.start_time ?? minSqlDate,
                    StatusUpdateTime = subscriptionDetails.status_update_time ?? minSqlDate,
                    SubscriberName = $"{subscriptionDetails.subscriber?.name?.given_name ?? string.Empty} {subscriptionDetails.subscriber?.name?.surname ?? string.Empty}".Trim(),
                    SubscriberEmail = subscriptionDetails.subscriber?.email_address ?? string.Empty,
                    PayerId = subscriptionDetails.subscriber?.payer_id ?? string.Empty,
                    OutstandingBalance = subscriptionDetails.billing_info?.outstanding_balance?.value != null
                        ? Convert.ToDecimal(subscriptionDetails.billing_info.outstanding_balance.value)
                        : 0,
                    OutstandingCurrency = subscriptionDetails.billing_info?.outstanding_balance?.currency_code ?? string.Empty,
                    NextBillingTime = subscriptionDetails.billing_info?.next_billing_time ?? minSqlDate,
                    LastPaymentTime = subscriptionDetails.billing_info?.last_payment?.time ?? minSqlDate,
                    LastPaymentAmount = subscriptionDetails.billing_info?.last_payment?.amount?.value != null
                        ? Convert.ToDecimal(subscriptionDetails.billing_info.last_payment.amount.value)
                        : 0,
                    LastPaymentCurrency = subscriptionDetails.billing_info?.last_payment?.amount?.currency_code ?? string.Empty,
                    FinalPaymentTime = subscriptionDetails.billing_info?.final_payment_time ?? minSqlDate,
                    CyclesCompleted = subscriptionDetails.billing_info?.cycle_executions != null && subscriptionDetails.billing_info.cycle_executions.Count > 0
                        ? subscriptionDetails.billing_info.cycle_executions[0].cycles_completed
                        : 0,
                    CyclesRemaining = subscriptionDetails.billing_info?.cycle_executions != null && subscriptionDetails.billing_info.cycle_executions.Count > 0
                        ? subscriptionDetails.billing_info.cycle_executions[0].cycles_remaining
                        : 0,
                    TotalCycles = subscriptionDetails.billing_info?.cycle_executions != null && subscriptionDetails.billing_info.cycle_executions.Count > 0
                        ? subscriptionDetails.billing_info.cycle_executions[0].total_cycles
                        : 0,
                    TrialIntervalUnit = plan?.TrialIntervalUnit,
                    TrialIntervalCount = plan?.TrialIntervalCount ?? 0,
                    TrialTotalCycles = plan?.TrialTotalCycles ?? 0,
                    TrialFixedPrice = plan?.TrialFixedPrice ?? 0
                };

                // Calcular la fecha del próximo pago después del período de prueba
                if (detallesSuscripcion.Status == "ACTIVE" && detallesSuscripcion.CyclesCompleted == 1 && detallesSuscripcion.CyclesRemaining == 0)
                {
                    detallesSuscripcion.NextBillingTime = detallesSuscripcion.StartTime.AddDays((double)detallesSuscripcion.TrialIntervalCount * (double)detallesSuscripcion.TrialTotalCycles + 1);
                }

                // Verificar si la suscripción ya existe en la base de datos
                var existingSubscription = await _policyExecutor.ExecutePolicyAsync(() => _context.SubscriptionDetails
                    .FirstOrDefaultAsync(s => s.SubscriptionId == detallesSuscripcion.SubscriptionId));

                if (existingSubscription != null)
                {
                    // Comparar los detalles y actualizar solo si han cambiado
                    bool hasChanges = !(
                        existingSubscription.PlanId == detallesSuscripcion.PlanId &&
                        existingSubscription.Status == detallesSuscripcion.Status &&
                        existingSubscription.StartTime == detallesSuscripcion.StartTime &&
                        existingSubscription.StatusUpdateTime == detallesSuscripcion.StatusUpdateTime &&
                        existingSubscription.SubscriberName == detallesSuscripcion.SubscriberName &&
                        existingSubscription.SubscriberEmail == detallesSuscripcion.SubscriberEmail &&
                        existingSubscription.PayerId == detallesSuscripcion.PayerId &&
                        existingSubscription.OutstandingBalance == detallesSuscripcion.OutstandingBalance &&
                        existingSubscription.OutstandingCurrency == detallesSuscripcion.OutstandingCurrency &&
                        existingSubscription.NextBillingTime == detallesSuscripcion.NextBillingTime &&
                        existingSubscription.LastPaymentTime == detallesSuscripcion.LastPaymentTime &&
                        existingSubscription.LastPaymentAmount == detallesSuscripcion.LastPaymentAmount &&
                        existingSubscription.LastPaymentCurrency == detallesSuscripcion.LastPaymentCurrency &&
                        existingSubscription.FinalPaymentTime == detallesSuscripcion.FinalPaymentTime &&
                        existingSubscription.CyclesCompleted == detallesSuscripcion.CyclesCompleted &&
                        existingSubscription.CyclesRemaining == detallesSuscripcion.CyclesRemaining &&
                        existingSubscription.TotalCycles == detallesSuscripcion.TotalCycles &&
                        existingSubscription.TrialIntervalUnit == detallesSuscripcion.TrialIntervalUnit &&
                        existingSubscription.TrialIntervalCount == detallesSuscripcion.TrialIntervalCount &&
                        existingSubscription.TrialTotalCycles == detallesSuscripcion.TrialTotalCycles &&
                        existingSubscription.TrialFixedPrice == detallesSuscripcion.TrialFixedPrice
                    );

                    if (hasChanges)
                    {
                        _context.SubscriptionDetails.Update(detallesSuscripcion);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.SubscriptionDetails.Add(detallesSuscripcion);
                    await _context.SaveChangesAsync();
                }

                // Verificar si ya existe una relación en UserSubscriptions
                var existeRelacion = await _policyExecutor.ExecutePolicyAsync(() => _context.UserSubscriptions
                    .FirstOrDefaultAsync(us => us.UserId == usuarioId && us.SubscriptionId == subscription_id));

                if (existeRelacion == null)
                {
                    var userSubscription = new UserSubscription
                    {
                        UserId = usuarioId,
                        SubscriptionId = subscription_id,
                        NombreSusbcriptor = detallesSuscripcion.SubscriberName,
                        PaypalPlanId = detallesSuscripcion.PlanId
                    };

                    _context.UserSubscriptions.Add(userSubscription);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "¡Suscripción confirmada con éxito!";
                return RedirectToAction("DetallesSuscripcion", new { id = subscription_id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar la suscripción con ID {SubscriptionId}", subscription_id);
                TempData["ErrorMessage"] = $"Error al confirmar la suscripción: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarSuscripcion([FromBody] PaypalRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.subscription_id))
                {
                    return BadRequest(new { success = false, errorMessage = "El ID de la suscripción es requerido." });
                }

                // Llamar al servicio para cancelar la suscripción en PayPal
                string result = await _paypalService.CancelarSuscripcion(request.subscription_id, "No satisfecho");

                // Obtener los detalles actualizados de la suscripción
                var actualizar = await _paypalRepository.ObtenerSubscripcion(request.subscription_id);
                if (actualizar != null)
                {
                    if (actualizar.Status == "ACTIVE" || actualizar.Status == "SUSPEND")
                    {
                        actualizar.Status = "CANCELLED";
                        await _context.UpdateEntityAsync(actualizar);
                    }
                }

                return Ok(new { success = true, message = "Suscripción cancelada con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, errorMessage = $"Error al cancelar la suscripción: {ex.Message}" });
            }
        }


        public async Task<IActionResult> DetallesSuscripcion(string id)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                // Validar el ID de la suscripción
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "El ID de la suscripción es requerido.";
                    return RedirectToAction("Error", "Home");
                }

                // Obtener detalles de la suscripción desde PayPal
                var subscriptionDetails = await _policyExecutor.ExecutePolicyAsync(() => _paypalService.ObtenerDetallesSuscripcion(id));
                if (subscriptionDetails == null)
                {
                    TempData["ErrorMessage"] = "No se pudieron obtener los detalles de la suscripción desde PayPal.";
                    return RedirectToAction("Error", "Home");
                }

                // Obtener el planId desde el DTO y validar que no sea nulo o vacío
                string planId = subscriptionDetails.plan_id;
                if (string.IsNullOrEmpty(planId))
                {
                    TempData["ErrorMessage"] = "El ID del plan de la suscripción es inválido o no se proporcionó.";
                    return RedirectToAction("Error", "Home");
                }

                // Obtener los detalles del plan desde la base de datos usando el PlanId
                var plan = await _policyExecutor.ExecutePolicyAsync(() => _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId));
                if (plan == null)
                {
                    // Intentar obtener los detalles del plan desde PayPal y mapear a PaypalPlanDetailsDto
                    var planResponse = await _policyExecutor.ExecutePolicyAsync(() => _paypalService.ObtenerDetallesPlan(planId));
                    var planDetails = new PaypalPlanDetailsDto
                    {
                        Id = planResponse.Id,
                        ProductId = planResponse.ProductId,
                        Name = planResponse.Name,
                        Description = planResponse.Description,
                        Status = planResponse.Status,
                        PaymentPreferences = new PaymentPreferencesDto
                        {
                            AutoBillOutstanding = planResponse.PaymentPreferences.AutoBillOutstanding,
                            SetupFee = planResponse.PaymentPreferences.SetupFee != null
                                ? new FixedPriceDto
                                {
                                    Value = planResponse.PaymentPreferences.SetupFee.Value.ToString(CultureInfo.InvariantCulture),
                                    CurrencyCode = planResponse.PaymentPreferences.SetupFee.CurrencyCode
                                }
                                : null,
                            SetupFeeFailureAction = planResponse.PaymentPreferences.SetupFeeFailureAction,
                            PaymentFailureThreshold = planResponse.PaymentPreferences.PaymentFailureThreshold
                        },
                        Taxes = planResponse.Taxes != null
                            ? new TaxesDto
                            {
                                Percentage = planResponse.Taxes.Percentage.ToString(CultureInfo.InvariantCulture),
                                Inclusive = planResponse.Taxes.Inclusive
                            }
                            : null,
                        BillingCycles = planResponse.BillingCycles.Select(b => new BillingCycleDto
                        {
                            TenureType = b.TenureType,
                            Sequence = b.Sequence,
                            Frequency = new FrequencyDto
                            {
                                IntervalUnit = b.Frequency.IntervalUnit,
                                IntervalCount = b.Frequency.IntervalCount
                            },
                            TotalCycles = b.TotalCycles,
                            PricingScheme = new PricingSchemeDto
                            {
                                FixedPrice = b.PricingScheme.FixedPrice != null
                                    ? new FixedPriceDto
                                    {
                                        Value = b.PricingScheme.FixedPrice.Value,
                                        CurrencyCode = b.PricingScheme.FixedPrice.CurrencyCode
                                    }
                                    : null
                            }
                        }).ToArray()
                    };

                    await _policyExecutor.ExecutePolicy(() => _paypalRepository.SavePlanDetailsAsync(planId, planDetails));
                    plan = await _policyExecutor.ExecutePolicyAsync(() => _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId));

                    // Si el plan sigue siendo nulo, no continuar
                    if (plan == null)
                    {
                        TempData["ErrorMessage"] = $"No se encontró un plan con el ID {planId} en la base de datos.";
                        return RedirectToAction("Error", "Home");
                    }
                }

                // Establecer la fecha mínima SQL
                var minSqlDate = new DateTime(1753, 1, 1);

                // Crear el objeto de detalles de la suscripción
                var detallesSuscripcion = new SubscriptionDetail
                {
                    SubscriptionId = subscriptionDetails.id ?? string.Empty,
                    PlanId = subscriptionDetails.plan_id ?? string.Empty,
                    Status = subscriptionDetails.status ?? string.Empty,
                    StartTime = subscriptionDetails.start_time ?? minSqlDate,
                    StatusUpdateTime = subscriptionDetails.status_update_time ?? minSqlDate,
                    SubscriberName = $"{subscriptionDetails.subscriber?.name?.given_name ?? string.Empty} {subscriptionDetails.subscriber?.name?.surname ?? string.Empty}".Trim(),
                    SubscriberEmail = subscriptionDetails.subscriber?.email_address ?? string.Empty,
                    PayerId = subscriptionDetails.subscriber?.payer_id ?? string.Empty,
                    OutstandingBalance = subscriptionDetails.billing_info?.outstanding_balance?.value != null
                        ? Convert.ToDecimal(subscriptionDetails.billing_info.outstanding_balance.value)
                        : 0,
                    OutstandingCurrency = subscriptionDetails.billing_info?.outstanding_balance?.currency_code ?? string.Empty,
                    NextBillingTime = subscriptionDetails.billing_info?.next_billing_time ?? minSqlDate,
                    LastPaymentTime = subscriptionDetails.billing_info?.last_payment?.time ?? minSqlDate,
                    LastPaymentAmount = subscriptionDetails.billing_info?.last_payment?.amount?.value != null
                        ? Convert.ToDecimal(subscriptionDetails.billing_info.last_payment.amount.value)
                        : 0,
                    LastPaymentCurrency = subscriptionDetails.billing_info?.last_payment?.amount?.currency_code ?? string.Empty,
                    FinalPaymentTime = subscriptionDetails.billing_info?.final_payment_time ?? minSqlDate,
                    CyclesCompleted = subscriptionDetails.billing_info?.cycle_executions != null && subscriptionDetails.billing_info.cycle_executions.Count > 0
                        ? subscriptionDetails.billing_info.cycle_executions[0].cycles_completed
                        : 0,
                    CyclesRemaining = subscriptionDetails.billing_info?.cycle_executions != null && subscriptionDetails.billing_info.cycle_executions.Count > 0
                        ? subscriptionDetails.billing_info.cycle_executions[0].cycles_remaining
                        : 0,
                    TotalCycles = subscriptionDetails.billing_info?.cycle_executions != null && subscriptionDetails.billing_info.cycle_executions.Count > 0
                        ? subscriptionDetails.billing_info.cycle_executions[0].total_cycles
                        : 0,
                    TrialIntervalUnit = plan?.TrialIntervalUnit,
                    TrialIntervalCount = plan?.TrialIntervalCount ?? 0,
                    TrialTotalCycles = plan?.TrialTotalCycles ?? 0,
                    TrialFixedPrice = plan?.TrialFixedPrice ?? 0
                };

                // Calcular la fecha del próximo pago después del período de prueba
                if (detallesSuscripcion.Status == "ACTIVE" && detallesSuscripcion.CyclesCompleted == 1 && detallesSuscripcion.CyclesRemaining == 0)
                {
                    detallesSuscripcion.NextBillingTime = detallesSuscripcion.StartTime.AddDays((double)detallesSuscripcion.TrialIntervalCount * (double)detallesSuscripcion.TrialTotalCycles + 1);
                }

                // Verificar si la suscripción ya existe en la base de datos
                var existingSubscription = await _policyExecutor.ExecutePolicyAsync(() => _context.SubscriptionDetails
                    .FirstOrDefaultAsync(s => s.SubscriptionId == detallesSuscripcion.SubscriptionId));

                if (existingSubscription != null)
                {
                    // Comparar los detalles y actualizar solo si han cambiado
                    bool hasChanges = !(
                        existingSubscription.PlanId == detallesSuscripcion.PlanId &&
                        existingSubscription.Status == detallesSuscripcion.Status &&
                        existingSubscription.StartTime == detallesSuscripcion.StartTime &&
                        existingSubscription.StatusUpdateTime == detallesSuscripcion.StatusUpdateTime &&
                        existingSubscription.SubscriberName == detallesSuscripcion.SubscriberName &&
                        existingSubscription.SubscriberEmail == detallesSuscripcion.SubscriberEmail &&
                        existingSubscription.PayerId == detallesSuscripcion.PayerId &&
                        existingSubscription.OutstandingBalance == detallesSuscripcion.OutstandingBalance &&
                        existingSubscription.OutstandingCurrency == detallesSuscripcion.OutstandingCurrency &&
                        existingSubscription.NextBillingTime == detallesSuscripcion.NextBillingTime &&
                        existingSubscription.LastPaymentTime == detallesSuscripcion.LastPaymentTime &&
                        existingSubscription.LastPaymentAmount == detallesSuscripcion.LastPaymentAmount &&
                        existingSubscription.LastPaymentCurrency == detallesSuscripcion.LastPaymentCurrency &&
                        existingSubscription.FinalPaymentTime == detallesSuscripcion.FinalPaymentTime &&
                        existingSubscription.CyclesCompleted == detallesSuscripcion.CyclesCompleted &&
                        existingSubscription.CyclesRemaining == detallesSuscripcion.CyclesRemaining &&
                        existingSubscription.TotalCycles == detallesSuscripcion.TotalCycles &&
                        existingSubscription.TrialIntervalUnit == detallesSuscripcion.TrialIntervalUnit &&
                        existingSubscription.TrialIntervalCount == detallesSuscripcion.TrialIntervalCount &&
                        existingSubscription.TrialTotalCycles == detallesSuscripcion.TrialTotalCycles &&
                        existingSubscription.TrialFixedPrice == detallesSuscripcion.TrialFixedPrice
                    );

                    if (hasChanges)
                    {
                        // Actualizar la entidad existente con los valores nuevos
                        existingSubscription.PlanId = detallesSuscripcion.PlanId;
                        existingSubscription.Status = detallesSuscripcion.Status;
                        existingSubscription.StartTime = detallesSuscripcion.StartTime;
                        existingSubscription.StatusUpdateTime = detallesSuscripcion.StatusUpdateTime;
                        existingSubscription.SubscriberName = detallesSuscripcion.SubscriberName;
                        existingSubscription.SubscriberEmail = detallesSuscripcion.SubscriberEmail;
                        existingSubscription.PayerId = detallesSuscripcion.PayerId;
                        existingSubscription.OutstandingBalance = detallesSuscripcion.OutstandingBalance;
                        existingSubscription.OutstandingCurrency = detallesSuscripcion.OutstandingCurrency;
                        existingSubscription.NextBillingTime = detallesSuscripcion.NextBillingTime;
                        existingSubscription.LastPaymentTime = detallesSuscripcion.LastPaymentTime;
                        existingSubscription.LastPaymentAmount = detallesSuscripcion.LastPaymentAmount;
                        existingSubscription.LastPaymentCurrency = detallesSuscripcion.LastPaymentCurrency;
                        existingSubscription.FinalPaymentTime = detallesSuscripcion.FinalPaymentTime;
                        existingSubscription.CyclesCompleted = detallesSuscripcion.CyclesCompleted;
                        existingSubscription.CyclesRemaining = detallesSuscripcion.CyclesRemaining;
                        existingSubscription.TotalCycles = detallesSuscripcion.TotalCycles;
                        existingSubscription.TrialIntervalUnit = detallesSuscripcion.TrialIntervalUnit;
                        existingSubscription.TrialIntervalCount = detallesSuscripcion.TrialIntervalCount;
                        existingSubscription.TrialTotalCycles = detallesSuscripcion.TrialTotalCycles;
                        existingSubscription.TrialFixedPrice = detallesSuscripcion.TrialFixedPrice;

                        _context.SubscriptionDetails.Update(existingSubscription);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.SubscriptionDetails.Add(detallesSuscripcion);
                    await _context.SaveChangesAsync();
                }

                return View(detallesSuscripcion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los detalles de la suscripción con ID {SubscriptionId}", id);
                TempData["ErrorMessage"] = $"Error al obtener los detalles de la suscripción: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }
        [HttpGet]
        public async Task<IActionResult> TodasSuscripciones([FromQuery] Paginacion paginacion)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                _logger.LogInformation("Página solicitada: {Pagina}, CantidadAMostrar: {Cantidad}", paginacion.Pagina, paginacion.CantidadAMostrar);

                // Consulta inicial para obtener los detalles de suscripción
                var queryable = from p in _context.SubscriptionDetails select p;

                // Calcular el número total de páginas
                var totalItems = await queryable.CountAsync();
                var totalPaginas = (int)Math.Ceiling((double)totalItems / paginacion.CantidadAMostrar);

                // Obtener las suscripciones con paginación
                var suscripciones = await _policyExecutor.ExecutePolicyAsync(() =>
                    Task.FromResult(queryable.Paginar(paginacion).ToList()));

                // Configurar paginación
                var paginacionConfig = new Paginacion
                {
                    Pagina = paginacion.Pagina,
                    CantidadAMostrar = paginacion.CantidadAMostrar,
                    TotalPaginas = totalPaginas,
                    Radio = 3
                };
                var paginas = _generarPaginas.GenerarListaPaginas(paginacionConfig);

                // Crear el modelo para la vista
                var model = new SuscripcionesPaginadosViewModel
                {
                    Suscripciones = suscripciones ?? new List<SubscriptionDetail>(),
                    Paginas = paginas,
                    TotalPaginas = totalPaginas,
                    PaginaActual = paginacion.Pagina,
                    TienePaginaSiguiente = paginacion.Pagina < totalPaginas,
                    TienePaginaAnterior = paginacion.Pagina > 1,
                    CantidadAMostrar = paginacion.CantidadAMostrar
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConnectionError"] = "El servidor ha tardado mucho en responder. Inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al obtener los datos de suscripción para la página {Pagina}", paginacion.Pagina);
                return RedirectToAction("Error", "Home");
            }
        }
        public async Task<IActionResult> ObtenerSuscripcionUsuario()
       {
          var usuarioActual = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
          int usuarioId;
          List<UserSubscription> suscripcionesUsuario = new List<UserSubscription>();
         if (usuarioActual != null)
          {
            if (int.TryParse(usuarioActual, out usuarioId))
             {
               // Consulta para obtener todas las suscripciones del usuario
               suscripcionesUsuario = await _context.UserSubscriptions
               .Include(x => x.User)
               .Where(x => x.UserId == usuarioId)
               .ToListAsync();
             }
          }

           return View(suscripcionesUsuario); 
        }

       

    }
}