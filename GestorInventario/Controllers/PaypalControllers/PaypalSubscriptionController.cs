using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Subscriptions;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.Subscription;
using GestorInventario.Application.Interfaces.Infrastructure.Common;
using GestorInventario.Application.Interfaces.Infrastructure.RetryPolicy;
using GestorInventario.Application.Interfaces.MetodosPaginacion;
using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Domain.Models;
using GestorInventario.Mappers;
using GestorInventario.Shared.Utilities;
using GestorInventario.ViewModels.Paypal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace GestorInventario.Controllers.PaypalControllers
{
    public class PaypalSubscriptionController : Controller
    {
        private readonly IPaypalSubscriptionService _paypalSubscriptionService;
        private readonly ILogger<PaypalSubscriptionController> _logger;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaginationService _paginationService;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ISubscriptionService _subscriptionService;

        public PaypalSubscriptionController(
            IPaypalSubscriptionService paypalSubscriptionService,
            ILogger<PaypalSubscriptionController> logger,
            IPolicyExecutor policyExecutor,
            ISubscriptionService subscriptionService,
            IUnitOfWork unitOfWork,
            IPaginationService paginationService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _paypalSubscriptionService = paypalSubscriptionService;
            _logger = logger;
            _policyExecutor = policyExecutor;
            _unitOfWork = unitOfWork;
            _paginationService = paginationService;
            _currentUserAccessor = currentUserAccessor;
            _subscriptionService = subscriptionService;
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
                var approvalUrl =
                    await _paypalSubscriptionService.Subscribirse(plan_id, returnUrl, cancelUrl, planName);
                if (!approvalUrl.Success)
                {
                    return RedirectToAction("Index", "Home");
                }


                return Redirect(approvalUrl.Data);
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

                int usuarioId = _currentUserAccessor.GetCurrentUserId();

                var subscriptionDetails = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalSubscriptionService.ObtenerDetallesSuscripcion(subscription_id));
                if (!subscriptionDetails.Success)
                {
                    TempData["ErrorMessage"] = "No se pudieron obtener los detalles de la suscripción desde PayPal.";
                    return RedirectToAction("Error", "Home");
                }

                string planId = subscriptionDetails.Data.PlanId ?? string.Empty;


                var detallesSuscripcion =
                    await _subscriptionService.CreateSubscriptionDetailAsync(subscriptionDetails.Data, planId);

                await _subscriptionService.SaveUserSubscriptionAsync(usuarioId, subscription_id,
                    detallesSuscripcion.SubscriberName, detallesSuscripcion.PlanId);

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

                var result = await _paypalSubscriptionService.CancelarSuscripcion(Id, Reason);


                await _subscriptionService.UpdateSubscriptionStatusAsync(result.Data.subId, result.Data.subStatus);

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

                var result = await _paypalSubscriptionService.SuspenderSuscripcion(Id, Reason);
                if (!result.IsSuccess)
                {
                    return RedirigirSegunRol();
                }

                // Update subscription status using the repository
                await _subscriptionService.UpdateSubscriptionStatusAsync(result.Data.subId, result.Data.subStatus);

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
                var result = await _paypalSubscriptionService.ActivarSuscripcion(Id, Reason);
                if (!result.Success)
                {
                    _logger.LogError("Error al realizar la peticion");
                    return RedirigirSegunRol();
                }

                await _subscriptionService.UpdateSubscriptionStatusAsync(result.Data.subId, result.Data.subStatus);
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

                // 1. Obtener datos frescos de PayPal (con política de reintentos)
                var subscriptionDetails = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalSubscriptionService.ObtenerDetallesSuscripcion(id));

                if (!subscriptionDetails.Success)
                {
                    TempData["ErrorMessage"] = "No se pudieron obtener los detalles desde PayPal.";
                    return RedirectToAction("Error", "Home");
                }

                // 2. Sincronizar/Crear el detalle en nuestra BD
                string planId = subscriptionDetails.Data.PlanId ?? string.Empty;
                if (string.IsNullOrEmpty(planId))
                {
                    _logger.LogWarning("El planId de la suscripción {SubscriptionId} es nulo o vacío", id);
                    TempData["ErrorMessage"] = "No se encontró el plan asociado a la suscripción.";
                    return RedirectToAction("Error", "Home");
                }

                var detallesSuscripcion = await _subscriptionService.CreateSubscriptionDetailAsync(subscriptionDetails.Data, planId);

                // 3. Mapear al ViewModel usando el nuevo Mapper
                var viewModel = PaypalSubscriptionMapper.ToViewModel(detallesSuscripcion);

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


                var paginationResult = await _paginationService.PaginarAsync(
                    query: queryable,
                    paginacion: paginacion
                );

                var model = new SubcriptionsIndexViewModel
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
                TempData["ConnectionError"] =
                    "El servidor ha tardado mucho en responder. Inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al obtener los datos de suscripción para la página {Pagina}",
                    paginacion.Pagina);
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
                var paginationResult = await _paginationService.PaginarAsync(
                    query: queryable,
                    paginacion: paginacion
                );

                // Construimos el modelo usando directamente el resultado del helper
                var model = new UserSubscriptionsIndexViewModel
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
                TempData["ConnectionError"] =
                    "El servidor ha tardado mucho en responder. Inténtelo de nuevo más tarde.";
                _logger.LogError(ex,
                    "Error al obtener las suscripciones del usuario {UsuarioId} para la página {Pagina}",
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