using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.Paypal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers.PaypalControllers
{
    public class PaypalSubscriptionController : Controller
    {
        private readonly IPaypalSubscriptionService _paypalSubscriptionService;
        private readonly ILogger<PaypalSubscriptionController> _logger;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaypalSubscriptionDetailService _paypalSubscriptionDetailService;
        private readonly IPaypalService _paypalService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaginationHelper _paginationHelper;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public PaypalSubscriptionController(
            IPaypalSubscriptionService paypalSubscriptionService, 
            ILogger<PaypalSubscriptionController> logger, 
            IPolicyExecutor policyExecutor, 
            IPaypalSubscriptionDetailService paypalSubscriptionDetailService,
            IPaypalService paypalService, 
            IUnitOfWork unitOfWork, 
            IPaginationHelper paginationHelper,
            ICurrentUserAccessor currentUserAccessor)
        {
            _paypalSubscriptionService = paypalSubscriptionService;
            _logger = logger;
            _policyExecutor = policyExecutor;
            _paypalSubscriptionDetailService = paypalSubscriptionDetailService;
            _paypalService = paypalService;
            _unitOfWork = unitOfWork;
            _paginationHelper = paginationHelper;
            _currentUserAccessor = currentUserAccessor;
        }

        //Metodo que inicia el proceso de suscripcion
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> IniciarSuscripcion(string plan_id, string planName)
        {
            // Define las URLs de retorno y cancelación
            string returnUrl = Url.Action("ConfirmarSuscripcion", "PaypalSubscription", null, Request.Scheme);
            string cancelUrl = Url.Action("CancelarSuscripcion", "PaypalSubscription", null, Request.Scheme);
            try
            {

                string approvalUrl = await _paypalSubscriptionService.Subscribirse(plan_id, returnUrl, cancelUrl, planName);


                return Redirect(approvalUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ocurrio un error con la api de paypa: {ex.Message}");
                TempData["ErrorMessage"] = $"Error al iniciar la suscripción";
                return RedirectToAction("MostrarPlanes", "PaypalPlan");

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

                var subscriptionDetails = await _policyExecutor.ExecutePolicyAsync(() => _paypalSubscriptionService.ObtenerDetallesSuscripcion(subscription_id));
                if (subscriptionDetails == null)
                {
                    TempData["ErrorMessage"] = "No se pudieron obtener los detalles de la suscripción desde PayPal.";
                    return RedirectToAction("Error", "Home");
                }

                string planId = subscriptionDetails.PlanId ?? string.Empty;


                var detallesSuscripcion = await _paypalSubscriptionDetailService.CreateSubscriptionDetailAsync(subscriptionDetails, planId);

                await _paypalService.SaveOrUpdateSubscriptionDetailsAsync(detallesSuscripcion);
                await _paypalService.SaveUserSubscriptionAsync(usuarioId, subscription_id, detallesSuscripcion.SubscriberName, detallesSuscripcion.PlanId);

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
                    return RedirigirSegunRol();
                }

                string result = await _paypalSubscriptionService.CancelarSuscripcion(Id, Reason);

                // Update subscription status using the repository
                await _paypalService.UpdateSubscriptionStatusAsync(Id, "CANCELLED");

                return RedirigirSegunRol();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar suscripción {Id}", Id);
                TempData["ErrorMessage"] = $"Error al cancelar la suscripción";
                return RedirigirSegunRol();
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
                    return RedirigirSegunRol();
                }

                if (string.IsNullOrWhiteSpace(Reason))
                    Reason = "Suspension manual por administrador";

                string result = await _paypalSubscriptionService.SuspenderSuscripcion(Id, Reason);

                // Update subscription status using the repository
                await _paypalService.UpdateSubscriptionStatusAsync(Id, "SUSPENDED");

                return RedirigirSegunRol();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al suspender suscripción {Id}", Id);
                TempData["ErrorMessage"] = $"Error al suspender la suscripción: {ex.Message}";
                return RedirigirSegunRol();
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
                return RedirigirSegunRol();
            }
            if (string.IsNullOrWhiteSpace(Reason))
                Reason = "Activación manual por administrador";
            try
            {
                string result = await _paypalSubscriptionService.ActivarSuscripcion(Id, Reason);
                await _paypalService.UpdateSubscriptionStatusAsync(Id, "ACTIVE");
                TempData["SuccessMessage"] = "Suscripción activada correctamente.";
                return RedirigirSegunRol();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar suscripción {Id}", Id);
                TempData["ErrorMessage"] = $"Error al activar la suscripción: {ex.Message}";
                return RedirigirSegunRol();
            }
        }


        [Authorize]
        public async Task<IActionResult> DetallesSuscripcion(string id)
        {
            CultureHelper.SetInvariantCulture();

            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("El id de la suscripción es requerido");
                    TempData["ErrorMessage"] = "ID de suscripción requerido.";
                    return RedirectToAction("Error", "Home");
                }

                // 1. Siempre obtener datos frescos de PayPal
                var subscriptionDetails = await _policyExecutor.ExecutePolicyAsync(
                    () => _paypalSubscriptionService.ObtenerDetallesSuscripcion(id));

                if (subscriptionDetails == null)
                {
                    TempData["ErrorMessage"] = "No se pudieron obtener los detalles desde PayPal.";
                    return RedirectToAction("Error", "Home");
                }

                // 2. Extraer planId
                string planId = subscriptionDetails.PlanId;
                if (string.IsNullOrEmpty(planId))
                {
                    _logger.LogWarning("El planId de la suscripción {SubscriptionId} es nulo o vacío", id);
                    TempData["ErrorMessage"] = "No se encontró el plan asociado a la suscripción.";
                    return RedirectToAction("Error", "Home");
                }

                // 3. Crear/actualizar el SubscriptionDetail 
                var detallesSuscripcion = await _paypalSubscriptionDetailService.CreateSubscriptionDetailAsync(subscriptionDetails, planId);



                int trialIntervalCount = detallesSuscripcion.TrialIntervalCount ?? 0;
                int trialTotalCycles = detallesSuscripcion.TrialTotalCycles ?? 0;
                int trialDays = trialIntervalCount > 0 ? trialIntervalCount * trialTotalCycles : 0;
                // PayPal no expone directamente si una suscripción está en período de prueba activo.
                // Se deduce de forma indirecta: si está activa, tiene próximo cobro programado,
                // ese cobro aún no ha ocurrido y nunca ha habido un pago previo,
                // entonces está en período de prueba.
                // LastPaymentTime <= 1900 indica que PayPal no registra ningún pago real aún.
                bool enPeriodoPruebaActivo =
                 detallesSuscripcion.Status == "ACTIVE" &&
                 detallesSuscripcion.NextBillingTime.HasValue &&
                 DateTime.UtcNow < detallesSuscripcion.NextBillingTime.Value &&
                 detallesSuscripcion.LastPaymentTime <= new DateTime(1900, 1, 1);

                var viewModel = new SuscripcionDetalleViewModel
                {
                    SubscriptionId = detallesSuscripcion.SubscriptionId,
                    PlanId = detallesSuscripcion.PlanId,
                    Status = detallesSuscripcion.Status,
                    SubscriberName = detallesSuscripcion.SubscriberName,
                    SubscriberEmail = detallesSuscripcion.SubscriberEmail,
                    PayerId = detallesSuscripcion.PayerId,
                    StartDate = detallesSuscripcion.StartTime,
                    StatusUpdateDate = detallesSuscripcion.StatusUpdateTime,

                    NextBillingTime = detallesSuscripcion.NextBillingTime,
                    LastPaymentTime = detallesSuscripcion.LastPaymentTime,
                    FinalPaymentTime = detallesSuscripcion.FinalPaymentTime,
                    OutstandingBalance = detallesSuscripcion.OutstandingBalance,
                    OutstandingCurrency = detallesSuscripcion.OutstandingCurrency,
                    LastPaymentAmount = detallesSuscripcion.LastPaymentAmount,
                    LastPaymentCurrency = detallesSuscripcion.LastPaymentCurrency,
                    TrialDays = trialDays,
                    CyclesCompleted = detallesSuscripcion.CyclesCompleted,
                    CyclesRemaining = detallesSuscripcion.CyclesRemaining,
                    TotalCycles = detallesSuscripcion.TotalCycles,
                    MostrarCiclos = detallesSuscripcion.CyclesCompleted > 0 ||
                                    detallesSuscripcion.CyclesRemaining > 0 ||
                                    detallesSuscripcion.TotalCycles > 0 ||
                                    trialDays > 0,
                    EnPeriodoPrueba = enPeriodoPruebaActivo

                };

                return View(viewModel);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles de suscripción {SubscriptionId}", id);
                TempData["ErrorMessage"] = "Error al obtener los detalles de la suscripción.";
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> TodasSuscripciones([FromQuery] Paginacion paginacion)
        {
            try
            {



                var queryable = _unitOfWork.PaypalRepository.ObtenerSubscripciones()
                                           .OrderByDescending(s => s.StartTime);


                var paginationResult = await _paginationHelper.PaginarAsync(
                    query: queryable,
                    paginacion: paginacion
                );

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
                var queryable = _unitOfWork.PaypalRepository.ObtenerSubscripcionesUsuario(usuarioActual);

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
        private IActionResult RedirigirSegunRol() =>
           User.IsInRole("Administrador")
               ? RedirectToAction(nameof(TodasSuscripciones))
               : RedirectToAction(nameof(ObtenerSuscripcionUsuario));
    }
}
