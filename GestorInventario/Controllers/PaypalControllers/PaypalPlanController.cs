using GestorInventario.Application.Exceptions;
using GestorInventario.Application.Mappers;

using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Shared.DTOS.Paypal.Requests.POST;
using GestorInventario.Shared.Utilities;

using GestorInventario.ViewModels.Paypal;
using GestorInventario.ViewModels.Productos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace GestorInventario.Controllers.PaypalControllers
{
    [Authorize]
    public class PaypalPlanController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaypalSubscriptionService _paypalSubscriptionService;     
        private readonly IPaginationHelper _paginationHelper;
        private readonly ILogger<PaypalPlanController> _logger;
        private readonly IPaypalService _paypalService;
        private readonly ISyncService _syncService;
        public PaypalPlanController(
            IUnitOfWork unitOfWork, 
            IPolicyExecutor policyExecutor, 
            IPaypalSubscriptionService paypalSubscriptionService,
          
            IPaginationHelper paginationHelper,
            ILogger<PaypalPlanController> logger,
            IPaypalService paypalService,
            ISyncService sync
            )
        {
            _unitOfWork = unitOfWork;
            _policyExecutor = policyExecutor;
            _paypalSubscriptionService = paypalSubscriptionService;
            _paginationHelper = paginationHelper;
            _logger = logger;
            _paypalService = paypalService;
            _syncService = sync;
           
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
                var plan = await _unitOfWork.PaypalRepository.ObtenerPlanPorIdAsync(request.PlanId);
                if (plan == null)
                {
                    return NotFound(new { success = false, errorMessage = $"No se encontró el plan con ID {request.PlanId}" });
                }

                // Obtener los detalles del plan desde PayPal para verificar la moneda
                var planDetails = await _policyExecutor.ExecutePolicyAsync(() => _paypalSubscriptionService.ObtenerDetallesPlan(request.PlanId));
                string planCurrency = planDetails.Data.BillingCycles?.FirstOrDefault(c => c.PricingScheme?.FixedPrice?.CurrencyCode != null)
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
                var result=  await _paypalSubscriptionService.UpdatePricingPlanAsync(request.PlanId, request.TrialAmount, request.RegularAmount, request.Currency);
                if (!result.Success)
                {
                    return BadRequest(new { success = false, errorMessage = $"No se ha podido actualizar el plan en este momento, intentelo de nuevo mas tarde" });
                }
                await _paypalService.SavePlanPriceUpdateAsync(result.Data.Item1, result.Data.Item2);
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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MostrarPlanes([FromQuery] Paginacion paginacion)
        {
            try
            {


                var queryable = _unitOfWork.PaypalRepository.ObtenerPlanes()
                .OrderByDescending(s => s.PaypalPlanId)
                .Select(p => p.ToPlanProjection()); 

                var paginationResult = await _paginationHelper.PaginarAsync(
                    query: queryable,
                    paginacion: paginacion
                );

                var monedas = await _policyExecutor.ExecutePolicyAsync(() =>
                    _unitOfWork.CarritoRepository.ObtenerMoneda());
              
                var model = new PlanesPaginadosViewModel
                {
                    Planes = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginationResult.PaginaActual,
                   
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

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> SincronizarPagina(int pagina)
        {
            var result = await _syncService.SyncPlansFromPayPalAsync(pagina);

            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new
            {
                success = true,
                message = result.Message,
                actualizados = result.Data 
            });
        }
        [Authorize]
        //Metodo que obtiene los datos necesarios antes de crear el producto al que se suscribira
        public async Task<IActionResult> CrearProductoYPlan()
        {
            var monedas = await _policyExecutor.ExecutePolicyAsync(
            () => _unitOfWork.CarritoRepository.ObtenerMoneda());

            var model = new ProductViewModelPaypal();
            ViewData["Moneda"] = new SelectList(monedas, "Codigo", "Codigo");
            // Obtener las categorías desde la enumeración y asignarlas al modelo
            model.Categories = _unitOfWork.PaypalRepository.GetCategoriesFromEnum();

            return View(model);
        }

        //Metodo que crea el producto  y plan al que se suscribira el usuario
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CrearProductoYPlan(ProductViewModelPaypal model, string monedaSeleccionada)
        {
            CultureHelper.SetInvariantCulture();

            var monedas = await _policyExecutor.ExecutePolicyAsync(() => _unitOfWork.CarritoRepository.ObtenerMoneda());


            ViewData["Moneda"] = new SelectList(monedas, "Codigo", "Codigo", monedaSeleccionada);
            model.Categories = _unitOfWork.PaypalRepository.GetCategoriesFromEnum();
            if (!ModelState.IsValid)
            {

                return View(model);
            }
            try
            {
                var product = await _paypalSubscriptionService.CreateProductAsync(
                    model.Name,
                    model.Description,
                    model.Type,
                    model.Category
                );

                if (product.Data == null)
                {
                    _logger.LogWarning("Hubo un error al crear el producto en PayPal");
                    TempData["ErrorMessage"] = "No se pudo crear el producto en PayPal.";
                    return View(model);
                }

                string productId = product.Data;

                var planResponse = await _paypalSubscriptionService.CreateSubscriptionPlanAsync(
                    productId,
                    model.PlanName,
                    model.PlanDescription,
                    model.Amount,
                    monedaSeleccionada,
                    model.IntervaUnit,
                    model.HasTrialPeriod ? model.TrialPeriodDays : 0,
                    model.HasTrialPeriod ? model.TrialAmount : 0.00m
                );
                if (!planResponse.Success)
                {
                    TempData["ErrorMessage"] = planResponse.Message;
                    return View(model);
                }
                var detallesPlan = await _paypalSubscriptionService.ObtenerDetallesPlan(planResponse.Data);
                if (detallesPlan.Success) {
                    await _paypalService.SavePlanDetailsAsync(planResponse.Data, detallesPlan.Data);

                }

                return RedirectToAction("MostrarProductos", "PaypalProduct");
               
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
                var activeSubscriptions = await _unitOfWork.PaypalRepository.ObtenerSuscriptcionesActivas(id);
                var userSubscriptions = await _unitOfWork.PaypalRepository.ObtenerSusbcripcionesUsuario(id);
                if (activeSubscriptions.Any() || userSubscriptions.Any())
                {
                    TempData["ErrorMessage"] = "No se puede desactivar el plan hay subscriptores activos";
                    return RedirectToAction(nameof(MostrarPlanes));
                }
                var result = await _paypalSubscriptionService.DesactivarPlan(id);
                await _paypalService.UpdatePlanStatusAsync(result.Data.planId, result.Data.planStatus);

                return RedirectToAction(nameof(MostrarPlanes), new { mensaje = result.Message });
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

                var activatePlan = await _paypalSubscriptionService.ActivarPlan(id);
                if (activatePlan.Success)
                {
                    await _paypalService.UpdatePlanStatusAsync(activatePlan.Data.planId, activatePlan.Data.planStatus);
                }
                return RedirectToAction(nameof(MostrarPlanes), new { mensaje = activatePlan.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar el producto y el plan: {ex.Message}");
            }
        }
    }
}
