using AutoMapper;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Response_paypal;
using GestorInventario.Application.DTOs.Response_paypal.Controller_Paypal_y_payment;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Application.Exceptions;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.ViewModels.product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class PaypalController : Controller
    {
       
       
        
        private readonly ILogger<PaypalController> _logger;
        private readonly IPaypalRepository _paypalRepository;
        private readonly ICarritoRepository _carritoRepository;
        private readonly IMapper _mapper;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaypalService _paypalService;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly IPaginationHelper _paginationHelper;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        public PaypalController(ILogger<PaypalController> logger, IConfiguration config, IUserRepository user, IPaginationHelper pagination,
        IPaypalRepository paypalController, ICarritoRepository carritoRepository, IMapper map, IPolicyExecutor executor, IPaypalService service, ICurrentUserAccessor current)
        {
            
            _paypalService= service;        
           _policyExecutor=executor;
            _logger = logger;
            _paypalRepository = paypalController;
            _carritoRepository = carritoRepository;
            _mapper = map;
            _configuration = config;
            _userRepository = user;
            _paginationHelper = pagination;
            _currentUserAccessor = current;
        }


        [HttpGet]
        public async Task<IActionResult> MostrarProductos([FromQuery] Paginacion paginacion)
        {
            try
            {
                if (!(_currentUserAccessor.IsAuthenticated()))
                {
                    return RedirectToAction("Login", "Auth");
                }

             

                // Obtener productos de PayPal
                var (respuestaProductos, tienePaginaSiguiente) = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalService.GetProductsAsync(paginacion.Pagina,paginacion.CantidadAMostrar));

                // Mapear productos a DTO
                var productos = respuestaProductos?.Products?.Select(p => new ProductoPaypalDto
                {
                    Id = p.Id,
                    Nombre = p.Name,
                    Descripcion = p.Description
                }).ToList() ?? new List<ProductoPaypalDto>();

                // Usar el helper para generar la paginación (modo "sin total real")
                var paginationResult = _paginationHelper.PaginarSinTotal(
                    items: productos,
                    paginaActual: paginacion.Pagina,
                    hasNextPage: tienePaginaSiguiente,
                    cantidadAMostrar: paginacion.CantidadAMostrar,
                    radio: paginacion.Radio
                );

                // Crear el ViewModel usando el resultado del helper
                var model = new ProductosPaypalViewModel
                {
                    Productos = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginationResult.PaginaActual,
                    TienePaginaSiguiente = paginationResult.Paginas.LastOrDefault()?.Habilitada ?? false,
                    TienePaginaAnterior = paginacion.Pagina > 1
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
        public async Task<IActionResult> MostrarPlanes([FromQuery] Paginacion paginacion)
        {
            try
            {
                if (!(_currentUserAccessor.IsAuthenticated()))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _logger.LogInformation("Página solicitada: {Pagina}, CantidadAMostrar: {Cantidad}", paginacion.Pagina, paginacion.CantidadAMostrar);

                // Obtener planes de PayPal
                var (plans, hasNextPage) = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalService.GetSubscriptionPlansAsyncV2(paginacion.Pagina, paginacion.CantidadAMostrar));

                // Mapear a ViewModel
                var planesViewModel = new List<PlanesDto>();

                if (plans != null)
                {
                    foreach (var plan in plans)
                    {
                        var viewModel = new PlanesDto
                        {
                            Id = plan.Id,
                            productId = plan.ProductId,
                            Name = plan.Name,
                            Description = plan.Description,
                            Status = plan.Status,
                            Usage_type = plan.UsageType,
                            CreateTime = plan.CreateTime,
                            Billing_cycles = _paypalRepository.MapBillingCycles(plan.BillingCycles),
                            Taxes = _paypalRepository.MapTaxes(plan.Taxes),
                            CurrencyCode = plan.BillingCycles?.FirstOrDefault()?.PricingScheme?.FixedPrice?.CurrencyCode ?? string.Empty,
                        };
                        planesViewModel.Add(viewModel);
                    }
                }
                else
                {
                    _logger.LogInformation("Hubo un valor nulo al obtene los planes");
                }

                    // ────────────────────────────────────────────────
                    // USAMOS EL HELPER → modo "sin total real" (PayPal)
                    // ────────────────────────────────────────────────
                    var paginationResult = _paginationHelper.PaginarSinTotal(
                        items: planesViewModel,
                        paginaActual: paginacion.Pagina,
                        hasNextPage: hasNextPage,
                        cantidadAMostrar: paginacion.CantidadAMostrar,
                        radio: paginacion.Radio
                    );

                // Crear el ViewModel final usando el resultado del helper
                var model = new PlanesPaginadosViewModel
                {
                    Planes = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginationResult.PaginaActual,
                    TienePaginaSiguiente = paginationResult.Paginas.LastOrDefault()?.Habilitada ?? false,
                    TienePaginaAnterior = paginacion.Pagina > 1,
                    CantidadAMostrar = paginacion.CantidadAMostrar
                };

                ViewBag.Monedas = new SelectList(
                    await _policyExecutor.ExecutePolicyAsync(() => _carritoRepository.ObtenerMoneda()),
                    "Codigo",
                    "Codigo"
                );

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "Error al obtener los planes de suscripción. Intenta de nuevo más tarde.";
                _logger.LogError(ex, "Error al obtener los planes de suscripción para la página {Pagina}", paginacion.Pagina);
                return RedirectToAction("Error", "Home");
            }
        }



        //Metodo que obtiene los datos necesarios antes de crear el producto al que se suscribira
        public async Task<IActionResult> CrearProductoYPlan()
        {
          
            var model = new ProductViewModelPaypal();
            ViewData["Moneda"] = new SelectList(await _policyExecutor.ExecutePolicyAsync(() => _carritoRepository.ObtenerMoneda()), "Codigo", "Codigo");
            // Obtener las categorías desde la enumeración y asignarlas al modelo
            model.Categories = _paypalRepository.GetCategoriesFromEnum();

            return View(model);
        }

        //Metodo que crea el producto  y plan al que se suscribira el usuario
        [HttpPost]
        public async Task<IActionResult> CrearProductoYPlan(ProductViewModelPaypal model, string monedaSeleccionada)
        {
            // Cambia la cultura actual del hilo a InvariantCulture
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            if (ModelState.IsValid)
            {
                try
                {
                    // Crear un producto usando los datos del formulario
                    var product = await _paypalService.CreateProductAsync(model.Name, model.Description, model.Type, model.Category);
                    if(product == null)
                    {
                        _logger.LogInformation("Hubo un error al crear el producto en paypal");
                    }
                    string productId = product.Id; 

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

            model.Categories = _paypalRepository.GetCategoriesFromEnum();
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
                    
                    var productResponse = await _paypalService.EditarProducto(id, model.Name, model.Description);
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

         
                var detallesSuscripcion = await _paypalRepository.CreateSubscriptionDetailAsync(subscriptionDetails, planId, _paypalService);

                await _paypalRepository.SaveOrUpdateSubscriptionDetailsAsync(detallesSuscripcion);
                await _paypalRepository.SaveUserSubscriptionAsync(usuarioId, subscription_id, detallesSuscripcion.SubscriberName, detallesSuscripcion.PlanId);

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
        public async Task<IActionResult> CancelarSuscripcion([FromBody] PaypalRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.subscription_id))
                {
                    return BadRequest(new { success = false, errorMessage = "El ID de la suscripción es requerido." });
                }

                string result = await _paypalService.CancelarSuscripcion(request.subscription_id, "No satisfecho");

                // Update subscription status using the repository
                await _paypalRepository.UpdateSubscriptionStatusAsync(request.subscription_id, "CANCELLED");

                return Ok(new { success = true, message = "Suscripción cancelada con éxito." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar la suscripción con ID {SubscriptionId}", request?.subscription_id);
                return StatusCode(500, new { success = false, errorMessage = $"Error al cancelar la suscripción: {ex.Message}" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspenderSuscripcion([FromBody] SuspendSubscriptionRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Id))
                {
                    return BadRequest(new { success = false, errorMessage = "El ID de la suscripción es requerido." });
                }
                if (string.IsNullOrEmpty(request?.Reason))
                {
                    return BadRequest(new { success = false, errorMessage = "El motivo de la suspensión es requerido." });
                }

                string result = await _paypalService.SuspenderSuscripcion(request.Id, request.Reason);

                // Update subscription status using the repository
                await _paypalRepository.UpdateSubscriptionStatusAsync(request.Id, "SUSPENDED");

                return Ok(new { success = true, message = "Suscripción suspendida con éxito." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al suspender la suscripción con ID {SubscriptionId}", request?.Id);
                return StatusCode(500, new { success = false, errorMessage = $"Error al suspender la suscripción: {ex.Message}" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivarSuscripcion([FromBody] SuspendSubscriptionRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Id))
                {
                    return BadRequest(new { success = false, errorMessage = "El ID de la suscripción es requerido." });
                }
                if (string.IsNullOrEmpty(request?.Reason))
                {
                    return BadRequest(new { success = false, errorMessage = "El motivo de la activación es requerido." });
                }

                string result = await _paypalService.ActivarSuscripcion(request.Id, request.Reason);

                // Update subscription status using the repository
                await _paypalRepository.UpdateSubscriptionStatusAsync(request.Id, "ACTIVE");

                return Ok(new { success = true, message = "Suscripción activada con éxito." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar la suscripción con ID {SubscriptionId}", request?.Id);
                return StatusCode(500, new { success = false, errorMessage = $"Error al activar la suscripción: {ex.Message}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> ActualizarPrecioPlan([FromBody] UpdatePlanPriceRequestDto request)
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

                // Crear y guardar SubscriptionDetail usando el repositorio
                var detallesSuscripcion = await _paypalRepository.CreateSubscriptionDetailAsync(subscriptionDetails, planId, _paypalService);
                await _paypalRepository.SaveOrUpdateSubscriptionDetailsAsync(detallesSuscripcion);

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
                if (!(_currentUserAccessor.IsAuthenticated()))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _logger.LogInformation(
                    "Página solicitada: {Pagina}, CantidadAMostrar: {Cantidad}",
                    paginacion.Pagina,
                    paginacion.CantidadAMostrar
                );

                // Consulta base (IQueryable)
                var queryable = _paypalRepository.ObtenerSubscripciones();

                // Usamos el helper para paginar (calcula todo: total, items, páginas)
                var paginationResult = await _paginationHelper.PaginarAsync(
                    query: queryable,
                    paginacion: paginacion
                );

                // Construimos el modelo usando directamente el resultado del helper
                var model = new SuscripcionesPaginadosViewModel
                {
                    Suscripciones = paginationResult.Items ?? new List<SubscriptionDetail>(),
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginationResult.PaginaActual,
                    TienePaginaSiguiente = paginationResult.PaginaActual < paginationResult.TotalPaginas,
                    TienePaginaAnterior = paginationResult.PaginaActual > 1,
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
                if (!(_currentUserAccessor.IsAuthenticated()))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var usuarioActual = _currentUserAccessor.GetCurrentUserId();

                _logger.LogInformation(
                    "Página solicitada: {Pagina}, CantidadAMostrar: {Cantidad}, UsuarioId: {UsuarioId}",
                    paginacion.Pagina,
                    paginacion.CantidadAMostrar,
                    usuarioActual
                );

                // Consulta base (IQueryable) filtrada por usuario
                var queryable = _paypalRepository.ObtenerSubscripcionesUsuario(usuarioActual);

                // Delegamos toda la paginación al helper
                var paginationResult = await _paginationHelper.PaginarAsync(
                    query: queryable,
                    paginacion: paginacion
                );

                // Construimos el modelo usando directamente el resultado del helper
                var model = new SuscripcionesUsuarioPaginadosViewModel
                {
                    Suscripciones = paginationResult.Items ?? new List<UserSubscription>(),
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginationResult.PaginaActual,
                    TienePaginaSiguiente = paginationResult.PaginaActual < paginationResult.TotalPaginas,
                    TienePaginaAnterior = paginationResult.PaginaActual > 1,
                    CantidadAMostrar = paginacion.CantidadAMostrar
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConnectionError"] = "El servidor ha tardado mucho en responder. Inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al obtener las suscripciones del usuario {UsuarioId} para la página {Pagina}",
                    _currentUserAccessor.GetCurrentUserId(), paginacion.Pagina);
                return RedirectToAction("Error", "Home");
            }
        }


    }
}