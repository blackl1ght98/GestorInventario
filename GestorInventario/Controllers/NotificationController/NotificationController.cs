using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Infrastructure.RetryPolicy;
using GestorInventario.Application.Interfaces.MetodosPaginacion;
using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Shared.Utilities;
using GestorInventario.ViewModels.Notification;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Controllers.NotificationController
{
    public class NotificationController : Controller
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaginationService _paginationService;
        public NotificationController(INotificationRepository notificationRepository, ICurrentUserAccessor currentUserAccessor, IPolicyExecutor policyExecutor, IPaginationService pagination)
        {
            _notificationRepository = notificationRepository;
            _currentUserAccessor = currentUserAccessor;
            _policyExecutor = policyExecutor;
            _paginationService = pagination; 
        }

        public async Task<IActionResult> Index(Paginacion paginacion)
        {
            int usuarioId= _currentUserAccessor.GetCurrentUserId();
            var queryable = _policyExecutor.ExecutePolicy(() =>
                   _notificationRepository.ObtenerNotificaciones(usuarioId));
            var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                     _paginationService.PaginarAsync(queryable, paginacion));
            var viewmodel = new NotificationViewmodel
            {
                Notificaciones = paginationResult.Items,
                Paginas = paginationResult.Paginas.ToList(),
                PaginaActual = paginationResult.PaginaActual,
                TotalPaginas = paginationResult.TotalPaginas,
            };
            return View(viewmodel);
        }
        public async Task<IActionResult> ListadoParcial(Paginacion paginacion)
        {
            int usuarioId = _currentUserAccessor.GetCurrentUserId();
            var queryable = _policyExecutor.ExecutePolicy(() =>
                   _notificationRepository.ObtenerNotificaciones(usuarioId));
            var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                     _paginationService.PaginarAsync(queryable, paginacion));
            var viewmodel = new NotificationViewmodel
            {
                Notificaciones = paginationResult.Items,
                Paginas = paginationResult.Paginas.ToList(),
                PaginaActual = paginationResult.PaginaActual,
                TotalPaginas = paginationResult.TotalPaginas,
            };
            return PartialView("_NotificacionesDropdown", viewmodel);
        }
        [HttpPost]
        public async Task<IActionResult> MarcarLeida(int id)
        {
                 
            var result = await _policyExecutor.ExecutePolicyAsync(() => _notificationRepository.MarcarNotificacionComoLeida(id));
            if (result.Success)
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Error","Home");
            }
        }
        [HttpPost]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            int usuarioId = _currentUserAccessor.GetCurrentUserId();

            var result = await _policyExecutor.ExecutePolicyAsync(() =>
                _notificationRepository.MarcarTodasNotificacionesComoLeidas(usuarioId));

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Error", "Home");
            }
        }
        //Idea de mandar notificaciones personalizadas
        //[HttpPost("alert")]
        //public async Task<IActionResult> SendAlert([FromBody] string msg)
        //{
        //    var success = await _notificationService.SendWhatsAppNotificationAsync(msg);
        //    return success ? Ok("Enviado") : BadRequest("Error al enviar");
        //}
    }
}
