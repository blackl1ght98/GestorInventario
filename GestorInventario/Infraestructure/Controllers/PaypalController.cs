using AutoMapper;
using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Response_paypal.Controller_Paypal_y_payment;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Application.Exceptions;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
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
        private readonly IConfiguration _configuration;
        public PaypalController(GestorInventarioContext context, GenerarPaginas generar,  ILogger<PaypalController> logger, IConfiguration config,
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
            _configuration = config;
        }

      
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

                // Mapear productos a ProductoPaypalViewModel
                var productos = respuestaProductos?.Products?.Select(p => new ProductoPaypalViewModel
                {
                    Id = p.Id,
                    Nombre = p.Name,
                    Descripcion = p.Description
                }).ToList() ?? new List<ProductoPaypalViewModel>();

                // Calcular total de páginas (PayPal no proporciona totalItems, usamos tienePaginaSiguiente)
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

                // Obtener planes tipados
                var (plans, hasNextPage) = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalService.GetSubscriptionPlansAsyncV2(pagina, cantidadAMostrar)); 

                var planesViewModel = new List<PlanesViewModel>();

                if (plans != null)
                {
                    foreach (var plan in plans)
                    {
                        var viewModel = new PlanesViewModel
                        {
                            id = plan.Id,
                            productId = plan.ProductId,
                            name = plan.Name,
                            description = plan.Description,
                            status = plan.Status,
                            usage_type = plan.UsageType,
                            createTime = plan.CreateTime,
                            billing_cycles = MapBillingCycles(plan.BillingCycles),
                            Taxes = MapTaxes(plan.Taxes),
                            CurrencyCode = plan.BillingCycles?.FirstOrDefault()?.PricingScheme?.FixedPrice?.CurrencyCode ?? string.Empty,
                        };
                        planesViewModel.Add(viewModel);
                    }
                }

                int totalPaginas = hasNextPage ? pagina + 1 : pagina;

                var paginacion = new Paginacion
                {
                    Pagina = pagina,
                    CantidadAMostrar = cantidadAMostrar,
                    TotalPaginas = totalPaginas,
                    Radio = 3
                };
                var paginas = _generarPaginas.GenerarListaPaginas(paginacion);

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
                ViewBag.Monedas = new SelectList(await _policyExecutor.ExecutePolicyAsync(() => _carritoRepository.ObtenerMoneda()), "Codigo", "Codigo");
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "Error al obtener los planes de suscripción. Intenta de nuevo más tarde.";
                _logger.LogError(ex, "Error al obtener los planes de suscripción para la página {Pagina}", pagina);
                return RedirectToAction("Error", "Home");
            }
        }

        // Mapear listas de BillingCycle tipadas
        private List<BillingCycle> MapBillingCycles(List<BillingCycle> billingCycles)
        {
            if (billingCycles == null)
                return new List<BillingCycle>();

            // Si los tipos coinciden exactamente, solo devolvemos o clonamos (si quieres evitar referencias directas)
            return billingCycles.Select(cycle => new BillingCycle
            {
                TenureType = cycle.TenureType,
                Sequence = cycle.Sequence,
                TotalCycles = cycle.TotalCycles,
                Frequency = MapFrequency(cycle.Frequency),
                PricingScheme = MapPricingScheme(cycle.PricingScheme)
            }).ToList();
        }

        private Frequency MapFrequency(Frequency frequency)
        {
            if (frequency == null)
                return null;

            return new Frequency
            {
                IntervalUnit = frequency.IntervalUnit,
                IntervalCount = frequency.IntervalCount
            };
        }

        private PricingScheme MapPricingScheme(PricingScheme pricingScheme)
        {
            if (pricingScheme == null)
                return null;

            return new PricingScheme
            {
                Version = pricingScheme.Version,
                FixedPrice = MapMoney(pricingScheme.FixedPrice),
                CreateTime = pricingScheme.CreateTime,
                UpdateTime = pricingScheme.UpdateTime
            };
        }

        private Money MapMoney(Money money)
        {
            if (money == null)
                return null;

            return new Money
            {
                CurrencyCode = money.CurrencyCode,
                Value = money.Value
            };
        }

        private Taxes MapTaxes(Taxes taxes)
        {
            if (taxes == null)
                return null;

            return new Taxes
            {
                Percentage = taxes.Percentage,
                Inclusive = taxes.Inclusive
            };
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

      //Metodo que crea el producto  y plan al que se suscribira el usuario
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
        public async Task<IActionResult> DesactivarPlan(string id)
        {
            try
            {
                var activeSubscriptions = await _paypalRepository.ObtenerSuscriptcionesActivas(id);
                var userSubscriptions = await _paypalRepository.SusbcripcionesUsuario(id);
                if (activeSubscriptions.Any() || userSubscriptions.Any())
                {
                    return StatusCode(400, "No se puede cancelar el plan porque hay suscriptores activos.");
                }
                var deleteResponse = await _paypalService.DesactivarPlan( id);


                return RedirectToAction(nameof(MostrarPlanes), new { mensaje = deleteResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar el producto y el plan: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> ActivarPlan(string id)
        {
            try
            {
                var activeSubscriptions = await _paypalRepository.ObtenerSuscriptcionesActivas(id);
                var userSubscriptions = await _paypalRepository.SusbcripcionesUsuario(id);
                if (activeSubscriptions.Any() || userSubscriptions.Any())
                {
                    return StatusCode(400, "No se puede cancelar el plan porque hay suscriptores activos.");
                }
                var activateResponse = await _paypalService.ActivarPlan(id);


                return RedirectToAction(nameof(MostrarPlanes), new { mensaje = activateResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar el producto y el plan: {ex.Message}");
            }
        }
      


      
        public IActionResult EditarProductoPaypal()
        {
            return View();
        }
       
        [HttpPost]
        public async Task<IActionResult> EditarProductoPaypal(string id, EditProductPaypal model)
        {

            if (ModelState.IsValid)
            {

                try
                {
                    
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

                string planId = subscriptionDetails.PlanId ?? string.Empty;
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
                    SubscriptionId = subscriptionDetails.Id ?? string.Empty,
                    PlanId = subscriptionDetails.PlanId ?? string.Empty,
                    Status = subscriptionDetails.Status ?? string.Empty,
                    StartTime = subscriptionDetails.StartTime ?? minSqlDate,
                    StatusUpdateTime = subscriptionDetails.StatusUpdateTime ?? minSqlDate,
                    SubscriberName = $"{subscriptionDetails.Subscriber?.Name?.GivenName ?? string.Empty} {subscriptionDetails.Subscriber?.Name?.Surname ?? string.Empty}".Trim(),
                    SubscriberEmail = subscriptionDetails.Subscriber?.EmailAddress ?? string.Empty,
                    PayerId = subscriptionDetails.Subscriber?.PayerId ?? string.Empty,
                    OutstandingBalance = subscriptionDetails.BillingInfo?.OutstandingBalance?.Value != null
                        ? Convert.ToDecimal(subscriptionDetails.BillingInfo.OutstandingBalance.Value)
                        : 0,
                    OutstandingCurrency = subscriptionDetails.BillingInfo?.OutstandingBalance?.CurrencyCode ?? string.Empty,
                    NextBillingTime = subscriptionDetails.BillingInfo?.NextBillingTime ?? minSqlDate,
                    LastPaymentTime = subscriptionDetails.BillingInfo?.LastPayment?.Time ?? minSqlDate,
                    LastPaymentAmount = subscriptionDetails.BillingInfo?.LastPayment?.Amount?.Value != null
                        ? Convert.ToDecimal(subscriptionDetails.BillingInfo.LastPayment.Amount.Value)
                        : 0,
                    LastPaymentCurrency = subscriptionDetails.BillingInfo?.LastPayment?.Amount?.CurrencyCode ?? string.Empty,
                    FinalPaymentTime = subscriptionDetails.BillingInfo?.FinalPaymentTime ?? minSqlDate,
                    CyclesCompleted = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].CyclesCompleted
                        : 0,
                    CyclesRemaining = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].CyclesRemaining
                        : 0,
                    TotalCycles = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].TotalCycles
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
        [HttpPost]
        public async Task<IActionResult> SuspenderSuscripcion([FromBody] SuspendSubscriptionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Id))
                {
                    return BadRequest(new { success = false, errorMessage = "El ID de la suscripción es requerido." });
                }
                if (string.IsNullOrEmpty(request.Reason))
                {
                    return BadRequest(new { success = false, errorMessage = "El motivo de la suspensión es requerido." });
                }

                
                string result = await _paypalService.SuspenderSuscripcion(request.Id, request.Reason);

                // Obtener los detalles actualizados de la suscripción
                var subscription = await _paypalRepository.ObtenerSubscripcion(request.Id);
                if (subscription != null)
                {
                    if (subscription.Status == "ACTIVE")
                    {
                        subscription.Status = "SUSPENDED";
                        await _context.UpdateEntityAsync(subscription);
                    }
                }

                return Ok(new { success = true, message = "Suscripción suspendida con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, errorMessage = $"Error al suspender la suscripción: {ex.Message}" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> ActivarSuscripcion([FromBody] SuspendSubscriptionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Id))
                {
                    return BadRequest(new { success = false, errorMessage = "El ID de la suscripción es requerido." });
                }
                if (string.IsNullOrEmpty(request.Reason))
                {
                    return BadRequest(new { success = false, errorMessage = "El motivo de la activacion es requerido." });
                }

               
                string result = await _paypalService.ActivarSuscripcion(request.Id, request.Reason);

                // Obtener los detalles actualizados de la suscripción
                var subscription = await _paypalRepository.ObtenerSubscripcion(request.Id);
                if (subscription != null)
                {
                    if (subscription.Status == "CANCELLED" || subscription.Status == "SUSPENDED")
                    {
                        subscription.Status = "ACTIVE";
                        await _context.UpdateEntityAsync(subscription);
                    }
                }

                return Ok(new { success = true, message = "Suscripción activada con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, errorMessage = $"Error al suspender la suscripción: {ex.Message}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> ActualizarPrecioPlan([FromBody] UpdatePlanPriceRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { success = false, errorMessage = string.Join("; ", errors) });
                }

                // Validar que el plan exista
                var plan = await _paypalRepository.ObtenerPlan(request.PlanId);
                if (plan == null)
                {
                    return NotFound(new { success = false, errorMessage = $"No se encontró el plan con ID {request.PlanId}" });
                }

                // Obtener los detalles del plan desde PayPal para verificar la moneda
                var planDetails = await _policyExecutor.ExecutePolicyAsync(() => _paypalService.ObtenerDetallesPlan(request.PlanId));
                string planCurrency = planDetails.BillingCycles?.FirstOrDefault(c => c.PricingScheme?.FixedPrice?.CurrencyCode != null)
                    ?.PricingScheme.FixedPrice.CurrencyCode;
                if (string.IsNullOrEmpty(planCurrency))
                {
                    return BadRequest(new { success = false, errorMessage = "No se pudo determinar la moneda del plan." });
                }

                // Validar que la moneda solicitada coincida con la del plan
                if (request.Currency != planCurrency)
                {
                    return BadRequest(new { success = false, errorMessage = $"La moneda {request.Currency} no coincide con la moneda del plan ({planCurrency})." });
                }

                // Llamar al servicio para actualizar el precio
                string result = await _paypalService.UpdatePricingPlanAsync(request.PlanId, request.TrialAmount, request.RegularAmount, request.Currency);

                TempData["SuccessMessage"] = "Precio del plan actualizado con éxito.";
                return Ok(new { success = true, message = "Precio del plan actualizado con éxito." });
            }
            catch (PayPalException ex) when (ex.Message.Contains("No se proporcionaron esquemas de precios válidos"))
            {
                return BadRequest(new { success = false, errorMessage = "No se proporcionaron precios diferentes a los actuales para actualizar el plan." });
            }
            catch (PayPalException ex) when (ex.Message.Contains("PRICING_SCHEME_UPDATE_NOT_ALLOWED"))
            {
                return BadRequest(new { success = false, errorMessage = "No se puede actualizar el precio de un plan activo con suscripciones asociadas. Por favor, crea un nuevo plan o actualiza las suscripciones individualmente." });
            }
            catch (PayPalException ex) when (ex.Message.Contains("CURRENCY_MISMATCH"))
            {
                return BadRequest(new { success = false, errorMessage = "La moneda no coincide con la moneda del plan." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, errorMessage = $"Error al actualizar el precio del plan: {ex.Message}" });
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
                string planId = subscriptionDetails.PlanId;
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
                    SubscriptionId = subscriptionDetails.Id ?? string.Empty,
                    PlanId = subscriptionDetails.PlanId ?? string.Empty,
                    Status = subscriptionDetails.Status ?? string.Empty,
                    StartTime = subscriptionDetails.StartTime ?? minSqlDate,
                    StatusUpdateTime = subscriptionDetails.StatusUpdateTime ?? minSqlDate,
                    SubscriberName = $"{subscriptionDetails.Subscriber?.Name?.GivenName ?? string.Empty} {subscriptionDetails.Subscriber?.Name?.Surname ?? string.Empty}".Trim(),
                    SubscriberEmail = subscriptionDetails.Subscriber?.EmailAddress ?? string.Empty,
                    PayerId = subscriptionDetails.Subscriber?.PayerId ?? string.Empty,
                    OutstandingBalance = subscriptionDetails.BillingInfo?.OutstandingBalance?.Value != null
                        ? Convert.ToDecimal(subscriptionDetails.BillingInfo.OutstandingBalance.Value)
                        : 0,
                    OutstandingCurrency = subscriptionDetails.BillingInfo?.OutstandingBalance?.CurrencyCode ?? string.Empty,
                    NextBillingTime = subscriptionDetails.BillingInfo?.NextBillingTime ?? minSqlDate,
                    LastPaymentTime = subscriptionDetails.BillingInfo?.LastPayment?.Time ?? minSqlDate,
                    LastPaymentAmount = subscriptionDetails.BillingInfo?.LastPayment?.Amount?.Value != null
                        ? Convert.ToDecimal(subscriptionDetails.BillingInfo.LastPayment.Amount.Value)
                        : 0,
                    LastPaymentCurrency = subscriptionDetails.BillingInfo?.LastPayment?.Amount?.CurrencyCode ?? string.Empty,
                    FinalPaymentTime = subscriptionDetails.BillingInfo?.FinalPaymentTime ?? minSqlDate,
                    CyclesCompleted = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].CyclesCompleted
                        : 0,
                    CyclesRemaining = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].CyclesRemaining
                        : 0,
                    TotalCycles = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].TotalCycles
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
        [HttpGet]
        public async Task<IActionResult> ObtenerSuscripcionUsuario([FromQuery] Paginacion paginacion)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var usuarioActual = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (usuarioActual == null || !int.TryParse(usuarioActual, out int usuarioId))
                {
                    TempData["ErrorMessage"] = "No se pudo identificar al usuario. Por favor, inicia sesión nuevamente.";
                    return RedirectToAction("Login", "Auth");
                }

                _logger.LogInformation("Página solicitada: {Pagina}, CantidadAMostrar: {Cantidad}, UsuarioId: {UsuarioId}",
                    paginacion.Pagina, paginacion.CantidadAMostrar, usuarioId);

                // Consulta inicial para obtener las suscripciones del usuario
                var queryable = _context.UserSubscriptions
                    .Include(x => x.User)
                    .Where(x => x.UserId == usuarioId);

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
                var model = new SuscripcionesUsuarioPaginadosViewModel
                {
                    Suscripciones = suscripciones ?? new List<UserSubscription>(),
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
              
                  
                return RedirectToAction("Error", "Home");
            }
        }


    }
}