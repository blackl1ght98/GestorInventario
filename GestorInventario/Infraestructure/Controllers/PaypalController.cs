using AutoMapper;
using GestorInventario.Application.DTOs.Response_paypal;
using GestorInventario.Application.DTOs.Response_paypal.Controller_Paypal_y_payment;
using GestorInventario.Application.Exceptions;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.ViewModels.product;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize]
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
        [Authorize]
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
                var resultadoMonedas = await _policyExecutor.ExecutePolicyAsync(
          () => _carritoRepository.ObtenerMoneda());
                var monedas = resultadoMonedas.Data;
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
                   monedas,
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


        [Authorize]
        //Metodo que obtiene los datos necesarios antes de crear el producto al que se suscribira
        public async Task<IActionResult> CrearProductoYPlan()
        {
            var resultadoMonedas = await _policyExecutor.ExecutePolicyAsync(
            () => _carritoRepository.ObtenerMoneda());
            var monedas = resultadoMonedas.Data;
            var model = new ProductViewModelPaypal();
            ViewData["Moneda"] = new SelectList(monedas, "Codigo", "Codigo");
            // Obtener las categorías desde la enumeración y asignarlas al modelo
            model.Categories = _paypalRepository.GetCategoriesFromEnum();

            return View(model);
        }

        //Metodo que crea el producto  y plan al que se suscribira el usuario
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CrearProductoYPlan(ProductViewModelPaypal model, string monedaSeleccionada)
        {
            // Cambia la cultura 
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            var resultadoMonedas = await _policyExecutor.ExecutePolicyAsync(() => _carritoRepository.ObtenerMoneda());
            var monedas = resultadoMonedas?.Data ?? new List<Monedum>(); 

            ViewData["Moneda"] = new SelectList(monedas, "Codigo", "Codigo", monedaSeleccionada);           
            model.Categories = _paypalRepository.GetCategoriesFromEnum();         
            if (!ModelState.IsValid)
            {
               
                return View(model);   
            }          
            try
            {
                var product = await _paypalService.CreateProductAsync(
                    model.Name,
                    model.Description,
                    model.Type,
                    model.Category
                );

                if (product == null)
                {
                    _logger.LogWarning("Hubo un error al crear el producto en PayPal");
                    TempData["ErrorMessage"] = "No se pudo crear el producto en PayPal.";
                    return View(model);
                }

                string productId = product.Id;

                string planResponse = await _paypalService.CreateSubscriptionPlanAsync(
                    productId,
                    model.PlanName,
                    model.PlanDescription,
                    model.Amount,
                    monedaSeleccionada,
                    model.HasTrialPeriod ? model.TrialPeriodDays : 0,
                    model.HasTrialPeriod ? model.TrialAmount : 0.00m
                );
                if (planResponse.Contains("\"error\""))
                {
                    TempData["ErrorMessage"] = $"Error al crear el plan: {planResponse}";
                    return View(model);
                }
                return RedirectToAction(nameof(MostrarProductos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear producto/plan");
                TempData["ErrorMessage"] = "Ocurrió un error inesperado. Inténtalo de nuevo.";
                return View(model);
            }
        }
        //Metodo para desactivar el plan
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DesactivarPlan(string id)
        {
            try
            {
                var activeSubscriptions = await _paypalRepository.ObtenerSuscriptcionesActivas(id);
                var userSubscriptions = await _paypalRepository.SusbcripcionesUsuario(id);
                if (activeSubscriptions.Any() || userSubscriptions.Any())
                {
                    TempData["ErrorMessage"] = "No se puede desactivar el plan hay subscriptores activos";
                    return RedirectToAction(nameof(MostrarPlanes));
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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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
                _logger.LogError($"Ocurrio un error con la api de paypa: {ex.Message}");
                TempData["ErrorMessage"] = $"Error al iniciar la suscripción";
                return RedirectToAction(nameof(MostrarPlanes));
            }
        }
        [Authorize]
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
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarSuscripcion(string Id, string Reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Id))
                {
                    _logger.LogError("ID de la susbcripcion requerido para poder cancelarla");
                    if (User.IsInRole("Administrador"))
                    {
                        return RedirectToAction(nameof(TodasSuscripciones));
                    }
                    else
                    {
                        return RedirectToAction(nameof(ObtenerSuscripcionUsuario));
                    }
                }

                string result = await _paypalService.CancelarSuscripcion(Id, Reason);

                // Update subscription status using the repository
                await _paypalRepository.UpdateSubscriptionStatusAsync(Id, "CANCELLED");

                if (User.IsInRole("Administrador"))
                {
                    return RedirectToAction(nameof(TodasSuscripciones));
                }
                else
                {
                    return RedirectToAction(nameof(ObtenerSuscripcionUsuario));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar suscripción {Id}", Id);
                TempData["ErrorMessage"] = $"Error al cancelar la suscripción";
                if (User.IsInRole("Administrador"))
                {
                    return RedirectToAction(nameof(TodasSuscripciones));
                }
                else
                {
                    return RedirectToAction(nameof(ObtenerSuscripcionUsuario));
                }
            }
        }
 

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspenderSuscripcion(string Id, string Reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Id))
                {
                    _logger.LogError("ID de la susbcripcion requerido para poder suspenderla");
                    if (User.IsInRole("Administrador"))
                    {
                        return RedirectToAction(nameof(TodasSuscripciones));
                    }
                    else
                    {
                        return RedirectToAction(nameof(ObtenerSuscripcionUsuario));
                    }
                }

                if (string.IsNullOrWhiteSpace(Reason))
                    Reason = "Suspension manual por administrador";

                string result = await _paypalService.SuspenderSuscripcion(Id, Reason);

                // Update subscription status using the repository
                await _paypalRepository.UpdateSubscriptionStatusAsync(Id, "SUSPENDED");

                if (User.IsInRole("Administrador"))
                {
                    return RedirectToAction(nameof(TodasSuscripciones));
                }
                else
                {
                    return RedirectToAction(nameof(ObtenerSuscripcionUsuario));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al suspender suscripción {Id}", Id);
                TempData["ErrorMessage"] = $"Error al suspender la suscripción: {ex.Message}";
                if (User.IsInRole("Administrador"))
                {
                    return RedirectToAction(nameof(TodasSuscripciones));
                }
                else
                {
                    return RedirectToAction(nameof(ObtenerSuscripcionUsuario));
                }
            }
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivarSuscripcion(string Id, string Reason)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                _logger.LogError("ID de la susbcripcion requerido para poder activarla");
                if (User.IsInRole("Administrador"))
                {
                    return RedirectToAction(nameof(TodasSuscripciones));
                }
                else
                {
                    return RedirectToAction(nameof(ObtenerSuscripcionUsuario));
                }
            }             
            if (string.IsNullOrWhiteSpace(Reason))
                Reason = "Activación manual por administrador"; 
            try
            {
                string result = await _paypalService.ActivarSuscripcion(Id, Reason);
                await _paypalRepository.UpdateSubscriptionStatusAsync(Id, "ACTIVE");

                TempData["SuccessMessage"] = "Suscripción activada correctamente.";
                if (User.IsInRole("Administrador"))
                {
                    return RedirectToAction(nameof(TodasSuscripciones));
                }
                else
                {
                    return RedirectToAction(nameof(ObtenerSuscripcionUsuario));
                }
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar suscripción {Id}", Id);
                TempData["ErrorMessage"] = $"Error al activar la suscripción: {ex.Message}";
                if (User.IsInRole("Administrador"))
                {
                    return RedirectToAction(nameof(TodasSuscripciones));
                }
                else
                {
                    return RedirectToAction(nameof(ObtenerSuscripcionUsuario));
                }
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
        [Authorize]
        public async Task<IActionResult> DetallesSuscripcion(string id)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                // Validar el ID de la suscripción
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogError("El id de la subscripcion es requerido");
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
                    _logger.LogError("El id del plan es nulo");
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
        [Authorize]
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
        [Authorize]
        public async Task<IActionResult> ObtenerSuscripcionUsuario([FromQuery] Paginacion paginacion)
        {
            try
            {
                

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